using System.Text;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public static class OrderEmailExtensions
    {
        public static async Task SendOrderConfirmationEmailAsync(this IEmailSender emailSender, Order order, string recipientEmail)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail) || recipientEmail.StartsWith("guest_") || !recipientEmail.Contains("@"))
            {
                return;
            }

            var subject = $"[Bách Hóa Xanh] Xác nhận đơn hàng thành công #{order.Id}";
            var sb = new StringBuilder();

            sb.Append(@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 650px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 12px; overflow: hidden; background-color: #ffffff; box-shadow: 0 4px 15px rgba(0,0,0,0.05);"">
    <div style=""background: linear-gradient(135deg, #008848 0%, #006837 100%); color: #ffffff; padding: 25px 20px; text-align: center;"">
        <h1 style=""margin: 0; font-size: 26px; font-weight: 800; letter-spacing: 0.5px;"">BÁCH HÓA XANH</h1>
        <p style=""margin: 8px 0 0; font-size: 16px; opacity: 0.95;"">🎉 Đặt hàng thành công!</p>
    </div>
    
    <div style=""padding: 25px 25px 15px;"">
        <p style=""font-size: 15px; color: #333333; margin-top: 0;"">Xin chào <strong style=""color: #008848;"">" + System.Net.WebUtility.HtmlEncode(order.FullName) + @"</strong>,</p>
        <p style=""font-size: 14px; color: #555555; line-height: 1.6;"">
            Cảm ơn bạn đã mua sắm tại <strong>Bách Hóa Xanh</strong>. Đơn hàng của bạn đã được hệ thống ghi nhận thành công và đang được chuẩn bị để giao tới bạn.
        </p>

        <div style=""background-color: #f8faf9; border-left: 4px solid #008848; padding: 15px; border-radius: 0 8px 8px 0; margin: 20px 0;"">
            <h3 style=""margin: 0 0 10px 0; font-size: 15px; color: #008848;"">📋 Thông Tin Đơn Hàng #" + order.Id + @"</h3>
            <table style=""width: 100%; font-size: 13px; color: #444; border-collapse: collapse;"">
                <tr><td style=""padding: 4px 0; width: 140px; font-weight: 600;"">Mã đơn hàng:</td><td>#" + order.Id + @"</td></tr>
                <tr><td style=""padding: 4px 0; font-weight: 600;"">Ngày đặt hàng:</td><td>" + order.OrderDate.ToString("dd/MM/yyyy HH:mm") + @"</td></tr>
                <tr><td style=""padding: 4px 0; font-weight: 600;"">Người nhận:</td><td>" + System.Net.WebUtility.HtmlEncode(order.FullName) + @" - " + System.Net.WebUtility.HtmlEncode(order.Phone) + @"</td></tr>
                <tr><td style=""padding: 4px 0; font-weight: 600;"">Địa chỉ giao:</td><td>" + System.Net.WebUtility.HtmlEncode(order.Address) + @"</td></tr>
                <tr><td style=""padding: 4px 0; font-weight: 600;"">Hình thức thanh toán:</td><td>" + System.Net.WebUtility.HtmlEncode(order.PaymentMethod) + @" (" + (order.IsPaid ? "<span style='color:#2e7d32;font-weight:bold;'>Đã thanh toán</span>" : "<span style='color:#e65100;font-weight:bold;'>Chưa thanh toán</span>") + @")</td></tr>
            </table>
        </div>

        <h3 style=""color: #008848; font-size: 16px; border-bottom: 2px solid #e8f5e9; padding-bottom: 8px; margin-top: 25px;"">🛒 Chi Tiết Sản Phẩm</h3>
        <table style=""width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 13px;"">
            <thead>
                <tr style=""background-color: #e8f5e9; color: #006837; text-align: left;"">
                    <th style=""padding: 10px; border-radius: 6px 0 0 6px;"">Sản phẩm</th>
                    <th style=""padding: 10px; text-align: center;"">Số lượng</th>
                    <th style=""padding: 10px; text-align: right;"">Đơn giá</th>
                    <th style=""padding: 10px; text-align: right; border-radius: 0 6px 6px 0;"">Thành tiền</th>
                </tr>
            </thead>
            <tbody>");

            if (order.OrderDetails != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    var prodName = item.Product?.Name ?? "Sản phẩm";
                    var totalItem = item.Quantity * item.Price;
                    sb.Append($@"
                <tr style=""border-bottom: 1px solid #eeeeee;"">
                    <td style=""padding: 10px; color: #333333; font-weight: 600;"">{System.Net.WebUtility.HtmlEncode(prodName)}</td>
                    <td style=""padding: 10px; text-align: center; color: #666666;"">{item.Quantity:0.##}</td>
                    <td style=""padding: 10px; text-align: right; color: #666666;"">{item.Price:N0}đ</td>
                    <td style=""padding: 10px; text-align: right; color: #333333; font-weight: 600;"">{totalItem:N0}đ</td>
                </tr>");
                }
            }

            sb.Append($@"
            </tbody>
        </table>

        <div style=""width: 100%; max-width: 320px; margin-left: auto; font-size: 13px; color: #444;"">
            <div style=""display: flex; justify-content: space-between; padding: 4px 0;"">
                <span>Giảm giá:</span>
                <span>{(order.DiscountAmount > 0 ? $"-{order.DiscountAmount:N0}đ" : "0đ")}</span>
            </div>
            <div style=""display: flex; justify-content: space-between; padding: 4px 0;"">
                <span>Phí vận chuyển:</span>
                <span>{(order.ShippingFee > 0 ? $"+{order.ShippingFee:N0}đ" : "Miễn phí")}</span>
            </div>
            <div style=""display: flex; justify-content: space-between; padding: 8px 0; border-top: 2px solid #008848; margin-top: 6px; font-size: 16px; font-weight: 800; color: #d32f2f;"">
                <span>TỔNG CỘNG:</span>
                <span>{order.TotalAmount:N0}đ</span>
            </div>
        </div>
    </div>

    <div style=""background-color: #f4f6f5; padding: 18px 20px; text-align: center; font-size: 12px; color: #777777; border-top: 1px solid #e0e0e0;"">
        <p style=""margin: 0 0 4px;"">Cảm ơn bạn đã tin tưởng mua sắm tại <strong>Bách Hóa Xanh</strong>!</p>
        <p style=""margin: 0;"">Nếu có câu hỏi hoặc cần hỗ trợ, xin vui lòng gọi hotline <strong>1900 1908</strong>.</p>
    </div>
</div>");

            try
            {
                await emailSender.SendEmailAsync(recipientEmail, subject, sb.ToString());
            }
            catch
            {
                // Silently ignore or log email errors so checkout flow does not break if SMTP fails
            }
        }
    }
}
