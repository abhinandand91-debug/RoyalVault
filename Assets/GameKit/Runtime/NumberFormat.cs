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

        // Writes value as credits into buffer from start without allocating; returns the index after the last character
        public static int Credits(long value, char[] buffer, int start)
        {
            if (value < 0)
            {
                buffer[start] = '-';
                start++;
                value = -value;
            }

            int digits = 1;
            for (long rest = value / 10; rest > 0; rest /= 10)
            {
                digits++;
            }

            int end = start + digits + (digits - 1) / 3;
            int index = end - 1;
            for (int written = 0; written < digits; written++)
            {
                if (written > 0 && written % 3 == 0)
                {
                    buffer[index] = ',';
                    index--;
                }

                buffer[index] = (char)('0' + value % 10);
                index--;
                value /= 10;
            }

            return end;
        }
    }
}
