using System.Collections.Generic;

namespace FileSorter;

public class StringSorter : IComparer<string>
{
    public int Compare(string s1, string s2)
    {
        if (s1 is null || s2 is null)
            return string.CompareOrdinal(s1 ?? "", s2 ?? "");

        var index1 = s1.IndexOf('.');
        var index2 = s2.IndexOf('.');

        if (index1 == -1 || index2 == -1)
            return string.CompareOrdinal(s1 ?? "", s2 ?? "");

        var number1 = long.Parse(s1[..index1]);

        var number2 = long.Parse(s2[..index2]);

        var result = string.CompareOrdinal(s1[(index1 + 1)..].TrimStart(), s2[(index2 + 1)..].TrimStart());
        return result == 0 ? number1.CompareTo(number2) : result;
    }
}