using System.Diagnostics;
using System.Text;

namespace FileSorter;

public class FileSorter
{
    private const string InputFileName = @"test100_1.txt";
    private const string OutputFileName = @"output.txt";
    private const int MaxBuffer = 268435456; // 256MB

    private long NumberOfLines { get; set; }
    private List<string> UnsortedFiles { get; } = new();
    private static readonly StringSorter Comparer = new();

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
        using var strReader = new StreamReader(InputFileName);

        while (true)
        {
            var readBytes = 0L;
            var line = strReader.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            var fileName = $"unsorted_{partition}.txt";
            UnsortedFiles.Add(fileName);
            using (var writer = new StreamWriter(fileName))
                while (true)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        break;
                    }

                    readBytes += Encoding.ASCII.GetByteCount(line);
                    writer.WriteLine(line);
                    NumberOfLines++;

                    if (readBytes > MaxBuffer)
                    {
                        break;
                    }

                    line = strReader.ReadLine();
                }

            partition++;
        }
    }

    private Task SortFiles()
    {
        //Sort each file
        async void Sorter(int count)
        {
            await SortFile(UnsortedFiles[count]);
        }

        Parallel.For(0, UnsortedFiles.Count, Sorter);
        return Task.CompletedTask;
    }

    private static Task SortFile(string fileName)
    {
        var strList = File.ReadAllLines(fileName).ToList();

        strList.Sort(Comparer);

        File.WriteAllLines(fileName, strList);
        return Task.CompletedTask;
    }

    public void Merge()
    {
        //Merge files
        if (File.Exists(OutputFileName))
        {
            File.Delete(OutputFileName);
        }

        var strReaderRows = new List<ReaderRow>();

        //get first row
        foreach (var file in UnsortedFiles)
        {
            var strReader = new StreamReader(file);
            strReaderRows.Add(new ReaderRow(strReader.ReadLine(), strReader));
        }

        // get minimal value
        // k-way merge
        var chunk = new List<string>();
        for (var i = 0; i < NumberOfLines; i++)
        {
            strReaderRows.Sort((row1, row2) => new StringSorter().Compare(row1.Row, row2.Row));
            var str = strReaderRows.First(x => !string.IsNullOrWhiteSpace(x.Row));
            chunk.Add(str.Row);
            str.Row = str.Reader.ReadLine();

            //reached end of file
            if (str.Row is null)
            {
                str.Reader.Dispose();
                File.AppendAllLines(OutputFileName, chunk);
                chunk.Clear();
            }
        }
    }

    public void DeleteTempFile()
    {
        foreach (var fileName in UnsortedFiles)
        {
            File.Delete(fileName);
        }
    }
}