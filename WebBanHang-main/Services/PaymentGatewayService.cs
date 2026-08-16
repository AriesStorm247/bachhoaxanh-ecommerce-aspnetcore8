using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public interface IPaymentGatewayService
    {
        string CreatePaymentUrl(HttpContext httpContext, Order order);
        bool ValidateVnPayReturn(IQueryCollection query);
        string CreateBankTransferCode(Order order);
        string CreateBankTransferQrUrl(Order order);
        Task<BankTransferVerificationResult> VerifyBankTransferAsync(Order order, CancellationToken cancellationToken = default);

        // POS-specific methods (use BHX_ prefix)
        string CreatePosBankTransferCode(Order order);
        string CreatePosBankTransferQrUrl(Order order);
        Task<BankTransferVerificationResult> VerifyPosBankTransferAsync(Order order, CancellationToken cancellationToken = default);
    }

    public class BankTransferVerificationResult
    {
        public bool IsConfirmed { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
    }

    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PaymentGatewayService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public string CreatePaymentUrl(HttpContext httpContext, Order order)
        {
            var gateway = _configuration["Payment:Gateway"] ?? "Demo";
            var tmnCode = _configuration["Payment:VnPay:TmnCode"];
            var hashSecret = _configuration["Payment:VnPay:HashSecret"];
            var baseUrl = _configuration["Payment:VnPay:BaseUrl"];

            if (!gateway.Equals("VNPay", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(tmnCode)
                || string.IsNullOrWhiteSpace(hashSecret)
                || string.IsNullOrWhiteSpace(baseUrl))
            {
                return BuildAbsoluteUrl(httpContext, $"/Payment/Demo?orderId={order.Id}");
            }

            var returnUrl = BuildAbsoluteUrl(httpContext, "/Payment/VnPayReturn");
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var amount = ((long)(GetExpectedVndAmount(order) * 100)).ToString(CultureInfo.InvariantCulture);

            var data = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode,
                ["vnp_Amount"] = amount,
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = $"Thanh toan don hang #{order.Id}",
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_TxnRef"] = order.Id.ToString(CultureInfo.InvariantCulture)
            };

            var query = BuildQueryString(data);
            var secureHash = ComputeHmacSha512(hashSecret, query);

            return $"{baseUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateVnPayReturn(IQueryCollection query)
        {
            var hashSecret = _configuration["Payment:VnPay:HashSecret"];
            var receivedHash = query["vnp_SecureHash"].ToString();

            if (string.IsNullOrWhiteSpace(hashSecret) || string.IsNullOrWhiteSpace(receivedHash))
            {
                return false;
            }

            var data = new SortedDictionary<string, string>();
            foreach (var item in query)
            {
                if (!item.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                    || item.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    || item.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                data[item.Key] = item.Value.ToString();
            }

            var rawData = BuildQueryString(data);
            var expectedHash = ComputeHmacSha512(hashSecret, rawData);

            return expectedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
        }

        public string CreateBankTransferCode(Order order)
        {
            var prefix = _configuration["Payment:BankTransfer:OrderPrefix"];
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "DH_";
            }

            return $"{prefix}{order.Id}";
        }

        public string CreatePosBankTransferCode(Order order)
        {
            var prefix = _configuration["Payment:BankTransfer:PosOrderPrefix"];
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "BHX_";
            }

            return $"{prefix}{order.Id}";
        }

        public string CreateBankTransferQrUrl(Order order)
        {
            return BuildVietQrUrl(order, CreateBankTransferCode(order));
        }

        public string CreatePosBankTransferQrUrl(Order order)
        {
            return BuildVietQrUrl(order, CreatePosBankTransferCode(order));
        }

        private string BuildVietQrUrl(Order order, string transferCode)
        {
            var bankCode = _configuration["Payment:BankTransfer:BankCode"] ?? "ACB";
            var accountNumber = _configuration["Payment:BankTransfer:AccountNumber"] ?? "34675617";
            var accountName = _configuration["Payment:BankTransfer:AccountName"] ?? "VO VAN PHU";
            var template = _configuration["Payment:BankTransfer:QrTemplate"] ?? "compact2";
            var amount = GetExpectedVndAmount(order).ToString("0", CultureInfo.InvariantCulture);

            return $"https://img.vietqr.io/image/{WebUtility.UrlEncode(bankCode)}-{WebUtility.UrlEncode(accountNumber)}-{WebUtility.UrlEncode(template)}.png"
                + $"?amount={amount}"
                + $"&addInfo={WebUtility.UrlEncode(transferCode)}"
                + $"&accountName={WebUtility.UrlEncode(accountName)}"
                + $"&t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        public async Task<BankTransferVerificationResult> VerifyBankTransferAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            return await VerifyTransferWithCodeAsync(order, CreateBankTransferCode(order), cancellationToken);
        }

        public async Task<BankTransferVerificationResult> VerifyPosBankTransferAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            return await VerifyTransferWithCodeAsync(order, CreatePosBankTransferCode(order), cancellationToken);
        }

        private async Task<BankTransferVerificationResult> VerifyTransferWithCodeAsync(
            Order order,
            string transferCode,
            CancellationToken cancellationToken = default)
        {
            var apiUrl = BuildBankTransferApiUrl(order);
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return new BankTransferVerificationResult
                {
                    IsConfirmed = false,
                    Message = "Chưa cấu hình API xác nhận chuyển khoản."
                };
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            AddBankTransferApiHeaders(request);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                return new BankTransferVerificationResult
                {
                    IsConfirmed = false,
                    Message = $"Không gọi được API ngân hàng: {ex.Message}"
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new BankTransferVerificationResult
                {
                    IsConfirmed = false,
                    Message = $"API ngân hàng trả về lỗi {(int)response.StatusCode}."
                };
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new BankTransferVerificationResult
                {
                    IsConfirmed = false,
                    Message = "API ngân hàng không trả dữ liệu giao dịch."
                };
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                return new BankTransferVerificationResult
                {
                    IsConfirmed = false,
                    Message = "API ngân hàng trả dữ liệu không đúng định dạng JSON."
                };
            }

            using (document)
            {
                var expectedAmount = GetExpectedVndAmount(order);
                var expectedCode = NormalizePaymentText(transferCode);

                foreach (var transaction in EnumerateJsonObjects(document.RootElement))
                {
                    if (!TryReadTransactionAmount(transaction, out var transactionAmount))
                    {
                        continue;
                    }

                    if (decimal.Round(transactionAmount, 0) != expectedAmount)
                    {
                        continue;
                    }

                    var searchText = NormalizePaymentText(ReadTransactionSearchText(transaction));
                    if (!searchText.Contains(expectedCode, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return new BankTransferVerificationResult
                    {
                        IsConfirmed = true,
                        Message = "Đã tìm thấy giao dịch chuyển khoản hợp lệ.",
                        TransactionId = TryReadString(transaction, "id")
                            ?? TryReadString(transaction, "reference")
                            ?? TryReadString(transaction, "reference_number")
                            ?? TryReadString(transaction, "transaction_id")
                    };
                }
            }

            return new BankTransferVerificationResult
            {
                IsConfirmed = false,
                Message = "Chưa tìm thấy giao dịch đúng mã đơn và đúng số tiền."
            };
        }

        private string BuildBankTransferApiUrl(Order order)
        {
            var apiUrl = _configuration["Payment:BankTransfer:ApiUrl"];
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                return string.Empty;
            }

            return apiUrl
                .Replace("{orderId}", order.Id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{amount}", GetExpectedVndAmount(order).ToString("0", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                .Replace("{code}", Uri.EscapeDataString(CreateBankTransferCode(order)), StringComparison.OrdinalIgnoreCase)
                .Replace("{fromDate}", order.OrderDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        private void AddBankTransferApiHeaders(HttpRequestMessage request)
        {
            var apiKey = _configuration["Payment:BankTransfer:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            var headerName = _configuration["Payment:BankTransfer:ApiHeaderName"] ?? "Authorization";
            var headerValue = _configuration["Payment:BankTransfer:ApiHeaderValue"];

            if (string.IsNullOrWhiteSpace(headerValue))
            {
                if (headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    var scheme = _configuration["Payment:BankTransfer:ApiAuthScheme"] ?? "Bearer";
                    headerValue = $"{scheme} {apiKey}";
                }
                else
                {
                    headerValue = apiKey;
                }
            }

            request.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        private static decimal GetExpectedVndAmount(Order order)
        {
            return decimal.Round(order.TotalAmount, 0, MidpointRounding.AwayFromZero);
        }

        private static IEnumerable<JsonElement> EnumerateJsonObjects(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                yield return element;

                foreach (var property in element.EnumerateObject())
                {
                    foreach (var child in EnumerateJsonObjects(property.Value))
                    {
                        yield return child;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var child in EnumerateJsonObjects(item))
                    {
                        yield return child;
                    }
                }
            }
        }

        private static bool TryReadTransactionAmount(JsonElement transaction, out decimal amount)
        {
            string[] amountFields =
            {
                "amount_in",
                "amountIn",
                "creditAmount",
                "credit_amount",
                "credit",
                "amount",
                "transactionAmount",
                "transaction_amount",
                "money",
                "value"
            };

            foreach (var field in amountFields)
            {
                if (TryReadDecimal(transaction, field, out amount) && amount > 0)
                {
                    return true;
                }
            }

            amount = 0;
            return false;
        }

        private static bool TryReadDecimal(JsonElement transaction, string propertyName, out decimal amount)
        {
            amount = 0;
            if (transaction.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in transaction.EnumerateObject())
            {
                if (!property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    return property.Value.TryGetDecimal(out amount);
                }

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var raw = property.Value.GetString();
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        return false;
                    }

                    raw = raw
                        .Replace("VND", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                        .Trim()
                        .Replace(" ", "");

                    if (Regex.IsMatch(raw, @"^\d{1,3}(\.\d{3})+$"))
                    {
                        return decimal.TryParse(raw.Replace(".", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                    }

                    if (Regex.IsMatch(raw, @"^\d{1,3}(,\d{3})+$"))
                    {
                        return decimal.TryParse(raw.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
                    }

                    return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
                        || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out amount);
                }
            }

            return false;
        }

        private static string ReadTransactionSearchText(JsonElement transaction)
        {
            if (transaction.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            var values = new List<string>();
            foreach (var property in transaction.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    values.Add(property.Value.GetString() ?? string.Empty);
                }
            }

            return string.Join(" ", values);
        }

        private static string? TryReadString(JsonElement transaction, string propertyName)
        {
            if (transaction.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in transaction.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }
            }

            return null;
        }

        private static string NormalizePaymentText(string value)
        {
            return Regex.Replace(value.ToUpperInvariant(), "[^A-Z0-9]", string.Empty);
        }

        private static string BuildAbsoluteUrl(HttpContext httpContext, string pathAndQuery)
        {
            return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{pathAndQuery}";
        }

        private static string BuildQueryString(SortedDictionary<string, string> data)
        {
            return string.Join("&", data.Select(item =>
                $"{WebUtility.UrlEncode(item.Key)}={WebUtility.UrlEncode(item.Value)}"));
        }

        private static string ComputeHmacSha512(string secret, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
