using System.Text;
using BenchmarkDotNet.Attributes;

public class FileSorter
{
    private const string InputFileName = @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\test.txt";

    private const string OutputFileName =
        @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\output.txt";

    private const int MaxBuffer = 10485760; // 1MB 

    static public void Main()
    {        
        var sort = new FileSorter();
        sort.Splitter();
    }

    [Benchmark]
    public  void Splitter()
    {
        using var fs = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        long remainBytes = fs.Length;
        long partition = 0;
        long numberOfLines = 0;
        var sortedFiles = new List<string>();

        while (remainBytes > 0)
        {
            var buffer = new byte[MaxBuffer + MaxBuffer / 100 * 10]; //add 10 percent for max buffer size
            var readBytes = 0L;
            byte bt = 0;
            while (readBytes < MaxBuffer || bt != '\n')
            {
                var b = fs.ReadByte();
                if (b == -1) //Reached end of file
                    break;
                bt = (byte) b;
                buffer[readBytes] = bt;
                readBytes++;
            }

            if (readBytes > 0)
            {
                var str = System.Text.Encoding.UTF8.GetString(buffer);
                var strReader = new StringReader(str);
                string line = "start";

                var strList = new List<string>();

                while (!string.IsNullOrWhiteSpace(line))
                {
                    line = strReader.ReadLine();
                    if (line is not null)
                    {
                        line = ReturnCleanASCII(line);

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            strList.Add(line);
                            numberOfLines++;
                        }
                    }
                }

                //Sort each file
                strList.Sort(new StringSorter());

                var fileName = $"sorted_{partition}.txt";
                sortedFiles.Add(fileName);

                using (StreamWriter writetext = new StreamWriter(fileName))
                {
                    foreach (var row in strList)
                    {
                        writetext.WriteLine(row);
                    }
                }

                partition++;
            }

            remainBytes -= readBytes;
        }

        //Merge files
        using (StreamWriter writetext = new StreamWriter(OutputFileName))
        {
            var strReaderRows = new List<ReaderRow>();

            //get first row
            foreach (var file in sortedFiles)
            {
                var strReader = new StreamReader(file);
                strReaderRows.Add(new ReaderRow(strReader.ReadLine(), strReader));
            }

            // get minimal value
            // k-way merge
            var k = 0L;
            while (k < numberOfLines)
            {
                strReaderRows.Sort((row1, row2) => new StringSorter().Compare(row1.Row, row2.Row));
                var str = strReaderRows.First(x => !string.IsNullOrWhiteSpace(x.Row));
                writetext.Write(str.Row + "\n");
                str.Row = str.Reader.ReadLine();
                if (str.Row is null)
                {
                    str.Reader.Close();
                }
                k++;
            }
        }

        //delete temp files
        foreach (var fileName in sortedFiles)
        {
            File.Delete(fileName);
        }
    }

    public static string ReturnCleanASCII(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if ((int) c > 127) // you probably don't want 127 either
                continue;
            if ((int) c < 32) // I bet you don't want control characters 
                continue;
            if (c == '%')
                continue;
            if (c == '?')
                continue;
            sb.Append(c);
        }

        return sb.ToString();
    }
}

public class StringSorter : IComparer<string>
{
    public int Compare(string s1, string s2)
    {
        string[] parts1 = s1.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        string[] parts2 = s2.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

        string string1 = parts1.Length > 1 ? parts1[1].TrimStart() : string.Empty;
        string string2 = parts2.Length > 1 ? parts2[1].TrimStart() : string.Empty;

        int result = string.Compare(string1, string2, StringComparison.Ordinal);
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
}

public class ReaderRow
{
    public string Row { get; set; }
    public StreamReader Reader { get; set; }

    public ReaderRow(string row, StreamReader reader)
    {
        Row = row;
        Reader = reader;
    }
}