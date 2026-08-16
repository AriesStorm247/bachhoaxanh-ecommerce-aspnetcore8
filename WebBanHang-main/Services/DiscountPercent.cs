using System.Globalization;

namespace WebBanHang.Services
{
    public static class DiscountPercent
    {
        private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

        public static decimal Normalize(decimal value)
        {
            if (value >= 1000m)
            {
                return Math.Round(value / 1000m, 3, MidpointRounding.AwayFromZero);
            }

            return value;
        }

        public static string Format(decimal value)
        {
            return Normalize(value).ToString("0.###", DisplayCulture);
        }

        public static string FormatForInput(decimal value)
        {
            return Normalize(value).ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
