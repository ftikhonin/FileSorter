using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]
public class FileSorter
{
    private const string InputFileName =
        @"C:\src\FileGenerator\FileGenerator\FileGenerator\bin\Debug\net7.0\test10.txt";

    private const string OutputFileName =
        @"C:\src\FileGenerator\FileGenerator\FileGenerator\bin\Debug\net7.0\output.txt";

    // private const int MaxBuffer = 10485760; // 10MB
    private const int MaxBuffer = 1048576; // 10MB
    private long NumberOfLines = 0;
    private ConcurrentBag<string> UnsortedFile = new ConcurrentBag<string>();

    static public void Main()
    {
        var summary = BenchmarkRunner.Run<FileSorter>();

        var timestamp1 = DateTime.Now;
        // Console.WriteLine(timestamp);

        var sort = new FileSorter();
        sort.Splitter();
        sort.Sorter();
        sort.Merger();
        // Console.WriteLine(timestamp);

        TimeSpan ts = DateTime.Now - timestamp1;
        Console.WriteLine("Result {0}", ts.TotalMilliseconds);
    }

    [Benchmark]
    public Task Splitter()
    {
        var timestamp1 = DateTime.Now;
        using var fs = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        var remainBytes = fs.Length;
        var partition = 0;

        Console.WriteLine("No 1. of Seconds (Difference) = {0}", (DateTime.Now - timestamp1).TotalMilliseconds);
        while (remainBytes > 0)
        {
            var buffer = new byte[MaxBuffer + MaxBuffer / 100 * 10]; //add 1 percent for max buffer size
            var readBytes = fs.Read(buffer, 0, buffer.Length);
            if (readBytes > 0)
            {
                var str = System.Text.Encoding.UTF8.GetString(buffer);
                var strReader = new StringReader(str);

                var line = "start";

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
                            NumberOfLines++;
                        }
                    }
                }

                //Sort each file
                // strList.Sort(new StringSorter());

                var fileName = $"unsorted_{partition}.txt";
                UnsortedFile.Add(fileName);
                File.WriteAllLines(fileName, strList);
                partition++;
            }

            remainBytes -= readBytes;
        }

        fs.Close();

        Console.WriteLine("No 1. of Seconds (Difference) = {0}", (DateTime.Now - timestamp1).TotalMilliseconds);
        return Task.CompletedTask;
    }

    public async Task Sorter()
    {
        var timestamp1 = DateTime.Now;
        Parallel.ForEach(UnsortedFile, fileName =>
        {
            using var streamReader = new StreamReader(fileName);
            var counter = 0;
            var unsortedRows = new List<string>();
            while (!streamReader.EndOfStream)
            {
                unsortedRows[counter++] = streamReader.ReadLine();
            }

            unsortedRows.Sort(new StringSorter());
            // await using var streamWriter = new StreamWriter(unsortedFile);
            File.WriteAllLines(fileName, unsortedRows);
        });
        // foreach (var fileName in UnsortedFile)
        // {
        //     using var streamReader = new StreamReader(fileName);
        //     var counter = 0;
        //     var unsortedRows = new List<string>();
        //     while (!streamReader.EndOfStream)
        //     {
        //         unsortedRows[counter++] = await streamReader.ReadLineAsync();
        //     }
        //
        //     unsortedRows.Sort(new StringSorter());
        //     // await using var streamWriter = new StreamWriter(unsortedFile);
        //     File.WriteAllLines(fileName, unsortedRows);
        // }
    }

    public async Task Merger()
    {
        var timestamp1 = DateTime.Now;
        //Merge files
        using (var writetext = new StreamWriter(OutputFileName))
        {
            var strReaderRows = new List<ReaderRow>();

            //get first row
            foreach (var file in UnsortedFile)
            {
                var strReader = new StreamReader(file);
                strReaderRows.Add(new ReaderRow(strReader.ReadLine(), strReader));
            }

            // get minimal value
            // k-way merge
            var k = 0L;
            while (k < NumberOfLines)
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

        Console.WriteLine("No 3. of Seconds (Difference) = {0}", (DateTime.Now - timestamp1).TotalMilliseconds);
        //delete temp files
        foreach (var fileName in UnsortedFile)
        {
            File.Delete(fileName);
        }
    }

    private async Task SortFile(string unsortedFile)
    {
        using var streamReader = new StreamReader(unsortedFile);
        var counter = 0;
        var unsortedRows = new List<string>();
        while (!streamReader.EndOfStream)
        {
            unsortedRows[counter++] = await streamReader.ReadLineAsync();
        }

        unsortedRows.Sort(new StringSorter());
        // await using var streamWriter = new StreamWriter(unsortedFile);
        File.WriteAllLines(unsortedFile, unsortedRows);
    }

    public static string ReturnCleanASCII(string s)
    {
        var sb = new StringBuilder(s.Length);
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
    // public int Compare(string s1, string s2)
    // {
    //     string[] parts1 = s1.Split('.');
    //     string[] parts2 = s2.Split('.');
    //
    //     string string1 = parts1[1].TrimStart();
    //     string string2 = parts2[1].TrimStart();
    //
    //     if (string1.Equals(string2))
    //     {
    //         double number1 = double.Parse(parts1[0]);
    //         double number2 = double.Parse(parts2[0]);
    //         return number1.CompareTo(number2);
    //     }
    //
    //     return string.Compare(string1, string2, StringComparison.Ordinal);
    // }

    public int Compare(string s1, string s2)
    {
        int dotIndex1 = s1.IndexOf('.');
        int dotIndex2 = s2.IndexOf('.');

        string string1 = s1.Substring(dotIndex1 + 1).TrimStart();
        string string2 = s2.Substring(dotIndex2 + 1).TrimStart();

        int result = string.Compare(string1, string2, StringComparison.OrdinalIgnoreCase);

        if (result == 0)
        {
            double number1, number2;
            if (double.TryParse(s1.Substring(0, dotIndex1), out number1) &&
                double.TryParse(s2.Substring(0, dotIndex2), out number2))
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