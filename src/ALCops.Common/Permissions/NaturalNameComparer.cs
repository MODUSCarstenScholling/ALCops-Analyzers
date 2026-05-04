namespace ALCops.Common.Permissions;

/// <summary>
/// Natural/alphanumeric string comparer for AL object names.
/// Splits strings into text and numeric chunks:
/// - Text chunks are compared with spaces stripped and InvariantCultureIgnoreCase.
/// - Numeric chunks are compared as integers.
/// - Shorter string wins as tiebreaker when all chunks are equal.
/// Functionally equivalent to AZ AL Dev Tools' AlphanumComparatorFast.
/// </summary>
public static class NaturalNameComparer
{
    /// <summary>
    /// Compares two object names using natural/alphanumeric sorting.
    /// Null or empty strings sort after non-empty strings.
    /// </summary>
    public static int Compare(string? x, string? y)
    {
        bool xEmpty = string.IsNullOrWhiteSpace(x);
        bool yEmpty = string.IsNullOrWhiteSpace(y);

        if (xEmpty && yEmpty)
            return 0;
        if (xEmpty)
            return 1;
        if (yEmpty)
            return -1;

        int len1 = x!.Length;
        int len2 = y!.Length;
        int marker1 = 0;
        int marker2 = 0;

        while (marker1 < len1 && marker2 < len2)
        {
            char ch1 = x[marker1];
            char ch2 = y[marker2];

            // Collect chunk from x
            int chunkStart1 = marker1;
            bool isDigit1 = char.IsDigit(ch1);
            while (marker1 < len1 && char.IsDigit(x[marker1]) == isDigit1)
                marker1++;

            // Collect chunk from y
            int chunkStart2 = marker2;
            bool isDigit2 = char.IsDigit(ch2);
            while (marker2 < len2 && char.IsDigit(y[marker2]) == isDigit2)
                marker2++;

            int result;
            if (isDigit1 && isDigit2)
            {
                // Both numeric: compare as integers
                long num1 = ParseLong(x, chunkStart1, marker1);
                long num2 = ParseLong(y, chunkStart2, marker2);
                result = num1.CompareTo(num2);
            }
            else
            {
                // At least one is text: compare with spaces stripped, InvariantCultureIgnoreCase
                var str1 = StripSpaces(x, chunkStart1, marker1);
                var str2 = StripSpaces(y, chunkStart2, marker2);
                result = string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase);
            }

            if (result != 0)
                return result;
        }

        return len1 - len2;
    }

    private static long ParseLong(string s, int start, int end)
    {
        long result = 0;
        for (int i = start; i < end; i++)
            result = result * 10 + (s[i] - '0');
        return result;
    }

    private static string StripSpaces(string s, int start, int end)
    {
        // Fast path: if no spaces, return substring directly
        bool hasSpace = false;
        for (int i = start; i < end; i++)
        {
            if (s[i] == ' ')
            {
                hasSpace = true;
                break;
            }
        }

        if (!hasSpace)
            return s.Substring(start, end - start);

        var chars = new char[end - start];
        int count = 0;
        for (int i = start; i < end; i++)
        {
            if (s[i] != ' ')
                chars[count++] = s[i];
        }

        return new string(chars, 0, count);
    }
}
