using System.Diagnostics;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace FileSorter;

public class FileSorter
{
    private const string InputFileName =
        // @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\test.txt";
        @"test.txt";

    private const string OutputFileName =
        // @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\output.txt";
        @"output.txt";

    // private const int MaxBuffer = 10485760; // 10MB 
    private const int MaxBuffer = 1048576; // 1MB 
    private long _numberOfLines { get; set; } = 0;
    private List<string> _unsortedFiles { get; set; } = new();

    public static void Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var sort = new FileSorter();
        sort.Splitter();
        sort.Merge();
        sort.DeleteTempFile();
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }

    public void Splitter()
    {
        using var fs = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        var remainBytes = fs.Length;
        var partition = 0;
        var strReader = new StreamReader(InputFileName);
        var strList = new List<string>();
        while (remainBytes > 0)
        {
            strList.Clear();
            var readBytes = 0L;
            var line = strReader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            while (!string.IsNullOrWhiteSpace(line))
            {
                line = ReturnCleanASCII(line);

                var byteCountLine = Encoding.ASCII.GetByteCount(line);
                readBytes += byteCountLine;
                remainBytes -= byteCountLine;

                if (!string.IsNullOrWhiteSpace(line))
                {
                    strList.Add(line);
                    _numberOfLines++;
                }


                if (readBytes > MaxBuffer)
                {
                    break;
                }

                line = strReader.ReadLine();
            }
            
            strList.Sort(new StringSorter());
            
            var fileName = $"unsorted_{partition}.txt";
            _unsortedFiles.Add(fileName);
            File.WriteAllLines(fileName, strList);

            partition++;
        }
    }

    public async void SortFiles()
    {
        await Task.Run(() =>
            {
                var strList = new List<string>();
                
                strList.Sort(new StringSorter());
            }

        );
        //Sort each file
        // 
    }

    public void Merge()
    {
        //Merge files
        using (var writetext = new StreamWriter(OutputFileName))
        {
            var strReaderRows = new List<ReaderRow>();

            //get first row
            foreach (var file in _unsortedFiles)
            {
                var strReader = new StreamReader(file);
                strReaderRows.Add(new ReaderRow(strReader.ReadLine(), strReader));
            }

            // get minimal value
            // k-way merge
            var k = 0L;
            while (k < _numberOfLines)
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
    }

    private void DeleteTempFile()
    {
        foreach (var fileName in _unsortedFiles)
        {
            File.Delete(fileName);
        }
    }

    public static string ReturnCleanASCII(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c > 127) // you probably don't want 127 either
                continue;
            if (c < 32) // I bet you don't want control characters 
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