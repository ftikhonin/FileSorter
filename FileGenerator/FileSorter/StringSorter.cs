namespace FileSorter;

public class StringSorter : IComparer<string>
{
    public int Compare(string s1, string s2)
    {
        if (s1 is not null && s2 is not null && s1.Contains('.') && s2.Contains('.'))
        {
            ReadOnlySpan<char> span1 = s1.AsSpan();
            ReadOnlySpan<char> span2 = s2.AsSpan();

            var index = span1.IndexOf('.');
            var number1 = double.Parse(span1.Slice(0, index));
            var string1 = span1.Slice(index + 1).TrimStart().ToString();

            index = span2.IndexOf('.');
            var number2 = double.Parse(span2.Slice(0, index));
            var string2 = span2.Slice(index + 1).TrimStart().ToString();

            var result = string.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);
            if (result == 0)
            {
                return number1.CompareTo(number2);
            }

            return result;
        }

        return string.Compare(s1 ?? "", s2 ?? "", StringComparison.OrdinalIgnoreCase);
    }
}