using System.Diagnostics;
using System.Text;

namespace FileSorter;

/// <summary>
/// Как ориентир по времени - 10 Гб файл сортируется около 9 минут (бывало быстрее), а 1 Гб файл сортируется в рамках минуты (самый быстрый результат - 26 секунд).
/// Дополнительный ориентир - при сортировке 1 Гб используется 2-2,5 Гб памяти. 
/// </summary>
public class FileSorter
{
    private const string InputFileName =
        // @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\test.txt";
        // @"test.txt";
        @"test100.txt";
    // @"test1024.txt";

    private const string OutputFileName =
        // @"C:\src\FileSorter\FileGenerator\FileGenerator\bin\Debug\net7.0\output.txt";
        @"output.txt";

    private const int MaxBuffer = 10485760; // 10MB 
    // private const int MaxBuffer = 104857600; // 100MB 

    // private const int MaxBuffer = 1048576; // 1MB 
    private long NumberOfLines { get; set; } = 0;
    private List<string> UnsortedFiles { get; } = new();

    public static async Task Main()
    {
        Stopwatch sw = Stopwatch.StartNew();
        var sort = new FileSorter();
        sort.Splitter();
        await sort.SortFiles();
        sort.Merge();
        sort.DeleteTempFile();
        sw.Stop();
        Console.WriteLine(sw.Elapsed);
    }

    private void Splitter()
    {
        var partition = 0;
        var strReader = new StreamReader(InputFileName);

        var strList = new List<string>();
        while (true)
        {
            strList.Clear();
            var readBytes = 0L;
            var line = strReader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            while (true)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                var byteCountLine = Encoding.ASCII.GetByteCount(line);
                readBytes += byteCountLine;
                strList.Add(line);
                NumberOfLines++;

                if (readBytes > MaxBuffer)
                {
                    break;
                }

                line = strReader.ReadLine();
            }

            var fileName = $"unsorted_{partition}.txt";
            UnsortedFiles.Add(fileName);
            File.WriteAllLines(fileName, strList);

            partition++;
        }
    }

    private Task SortFiles()
    {
        //Sort each file
        Parallel.For(0, UnsortedFiles.Count, async count => { await SortFile(UnsortedFiles[count]); });
        return Task.CompletedTask;
    }

    private Task SortFile(string fileName)
    {
        var strList = File.ReadAllLines(fileName).ToList();
        strList.Sort(new StringSorter());

        File.WriteAllLines(fileName, strList);
        return Task.CompletedTask;
    }

    private void Merge()
    {
        //Merge files
        using (var writetext = new StreamWriter(OutputFileName))
        {
            var strReaderRows = new List<ReaderRow>();

            //get first row
            foreach (var file in UnsortedFiles)
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
    }

    private void DeleteTempFile()
    {
        foreach (var fileName in UnsortedFiles)
        {
            File.Delete(fileName);
        }
    }
}