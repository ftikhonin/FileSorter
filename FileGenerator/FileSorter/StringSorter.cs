public class StringSorter : IComparer<string>
{
    public int Compare(string s1, string s2)
    {
        var result = 0;
        if (s1 is not null && s2 is not null && s1.Contains('.') && s2.Contains('.'))
        {
            string[] parts1 = s1.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            string[] parts2 = s2.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

            string string1 = parts1.Length > 1 ? parts1[1].TrimStart() : string.Empty;
            string string2 = parts2.Length > 1 ? parts2[1].TrimStart() : string.Empty;

            result = string.Compare(string1, string2, StringComparison.Ordinal);
            if (result == 0 && parts1.Length > 0 && parts2.Length > 0)
            {
                double number1, number2;
                if (double.TryParse(parts1[0], out number1) && double.TryParse(parts2[0], out number2))
                {
                    return number1.CompareTo(number2);
                }
            }

            return result;
        }

        return string.Compare(s1 ?? "", s2 ?? "", StringComparison.Ordinal);
    }
}