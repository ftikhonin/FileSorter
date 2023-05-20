using System.Text;
using BenchmarkDotNet.Attributes;

namespace FileSorter;

public class FileSorter
{
    private const string InputFileName = @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\test.txt";

    private const string OutputFileName =
        @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\output.txt";

    private const int MaxBuffer = 10485760; // 1MB 

    public static void Main()
    {
        var sort = new FileSorter();
        sort.Splitter();
    }
    
    public void Splitter()
    {
        using var fs = new FileStream(InputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        var remainBytes = fs.Length;
        var partition = 0;
        var numberOfLines = 0L;
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
                            numberOfLines++;
                        }
                    }
                }

                //Sort each file
                strList.Sort(new StringSorter());

                var fileName = $"sorted_{partition}.txt";
                sortedFiles.Add(fileName);

                using (var writetext = new StreamWriter(fileName))
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
        using (var writetext = new StreamWriter(OutputFileName))
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