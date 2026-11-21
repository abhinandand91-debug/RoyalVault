using System.Globalization;

namespace GameKit
{
    // How credits are written everywhere in the game: 48,250 (the same in every locale)
    public static class NumberFormat
    {
        public static string Credits(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
