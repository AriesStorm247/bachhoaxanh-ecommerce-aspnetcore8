using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WebBanHang.Data;
using WebBanHang.Models;

namespace WebBanHang.Services
{
    public class DiscountCalculationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Code { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public Discount? Discount { get; set; }
    }

    public interface IDiscountService
    {
        Task<DiscountCalculationResult> CalculateDiscountAsync(string? code, decimal subtotal, bool trackDiscount = false);
    }

    public class DiscountService : IDiscountService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DiscountService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DiscountCalculationResult> CalculateDiscountAsync(string? code, decimal subtotal, bool trackDiscount = false)
        {
            var result = new DiscountCalculationResult
            {
                Subtotal = subtotal,
                FinalTotal = subtotal
            };

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Message = "Vui lòng nhập mã giảm giá!";
                return result;
            }

            var normalizedCode = code.Trim().ToUpper();
            var now = PromotionService.GetVietnamNow();

            IQueryable<Discount> query = _context.Discounts;
            if (!trackDiscount)
            {
                query = query.AsNoTracking();
            }

            var discount = await query.FirstOrDefaultAsync(x =>
                x.Code.ToUpper() == normalizedCode
                && x.IsSee
                && x.StartDate <= now
                && x.EndDate >= now);

            if (discount == null)
            {
                result.Message = "Mã giảm giá không hợp lệ hoặc đã hết hạn!";
                return result;
            }

            if (!string.IsNullOrWhiteSpace(discount.UserId))
            {
                var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(currentUserId) || discount.UserId != currentUserId)
                {
                    result.Message = "Mã giảm giá này không thuộc về tài khoản của bạn!";
                    return result;
                }
            }

            if (discount.Quantity <= 0)
            {
                result.Message = "Mã giảm giá đã hết lượt sử dụng!";
                return result;
            }

            if (subtotal < discount.MinOrderValue)
            {
                result.Message = $"Đơn hàng tối thiểu phải từ {discount.MinOrderValue:N0} đ";
                return result;
            }

            if (discount.Code.ToUpper().Contains("FREESHIP") || discount.Code.ToUpper().Contains("FREE_SHIP") || discount.Code.ToUpper().Contains("FREE-SHIP"))
            {
                result.Success = true;
                result.Message = "Áp dụng mã giảm giá miễn phí vận chuyển thành công!";
                result.Code = discount.Code;
                result.DiscountAmount = 0m;
                result.FinalTotal = subtotal;
                result.Discount = discount;
                return result;
            }

            var discountPercent = DiscountPercent.Normalize(discount.DiscountValue);
            if (discountPercent <= 0 || discountPercent > 100)
            {
                result.Message = "Mã giảm giá đang được cấu hình chưa đúng.";
                return result;
            }

            var discountAmount = subtotal * discountPercent / 100m;
            if (discount.MaxDiscount > 0 && discountAmount > discount.MaxDiscount)
            {
                discountAmount = discount.MaxDiscount;
            }

            discountAmount = Math.Min(discountAmount, subtotal);
            var finalTotal = Math.Max(0, subtotal - discountAmount);

            result.Success = true;
            result.Message = "Áp dụng mã giảm giá thành công!";
            result.Code = discount.Code;
            result.DiscountAmount = Math.Round(discountAmount, 0);
            result.FinalTotal = Math.Round(finalTotal, 0);
            result.Discount = discount;

            return result;
        }
    }
}
