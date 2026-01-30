using System.Globalization;

namespace ControlInventario.Helpers
{
    public static class CurrencyHelper
    {
        private static readonly CultureInfo GuatemalaCulture = CreateGuatemalaCulture();

        private static CultureInfo CreateGuatemalaCulture()
        {
            var culture = new CultureInfo("es-GT");
            culture.NumberFormat.CurrencySymbol = "Q";
            culture.NumberFormat.CurrencyDecimalDigits = 2;
            culture.NumberFormat.CurrencyDecimalSeparator = ".";
            culture.NumberFormat.CurrencyGroupSeparator = ",";
            culture.NumberFormat.NumberDecimalDigits = 2;
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = ",";
            return culture;
        }

        public static string ToQuetzales(decimal amount)
        {
            return amount.ToString("C", GuatemalaCulture);
        }

        public static string ToQuetzales(decimal? amount)
        {
            if (!amount.HasValue) return "Q0.00";
            return amount.Value.ToString("C", GuatemalaCulture);
        }

        public static string ToQuetzales(double amount)
        {
            return ((decimal)amount).ToString("C", GuatemalaCulture);
        }

        public static string ToQuetzales(double? amount)
        {
            if (!amount.HasValue) return "Q0.00";
            return ((decimal)amount.Value).ToString("C", GuatemalaCulture);
        }

        public static string ToQuetzales(int amount)
        {
            return ((decimal)amount).ToString("C", GuatemalaCulture);
        }

        public static string ToQuetzales(int? amount)
        {
            if (!amount.HasValue) return "Q0.00";
            return ((decimal)amount.Value).ToString("C", GuatemalaCulture);
        }

        public static decimal ParseQuetzales(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount))
                return 0;

            // Limpiar el string: remover "Q" y espacios
            var cleanAmount = amount.Replace("Q", "").Replace("¤", "").Trim();
            
            if (decimal.TryParse(cleanAmount, NumberStyles.Currency, GuatemalaCulture, out decimal result))
                return result;
            
            // Si falla, intentar sin formato de moneda
            if (decimal.TryParse(cleanAmount, out decimal result2))
                return result2;
            
            return 0;
        }

        public static string FormatAmount(decimal amount, string currency = "Q")
        {
            return $"{currency}{amount.ToString("N2", GuatemalaCulture)}";
        }
    }
}
