using System.Security.Cryptography;
using System.Text;

class FileGenerator
{
    private const string FileName = @"test.txt";
    private const char NewLine = '\n';


    static public void Main(String[] args)
    {
        // TODO: do a benchmark
        GenerateFile();
    }

    public static void GenerateFile()
    {
        Console.WriteLine("Enter a file size in MB");
        var sizeStr = Console.ReadLine();
        double size = 0;
        while (!double.TryParse(sizeStr, out size))
        {
            Console.WriteLine("Enter a file size in MB");
            sizeStr = Console.ReadLine();
        }

        // Check if file already exists. If yes, delete it.     
        if (File.Exists(FileName))
        {
            File.Delete(FileName);
        }

        List<string> dict = new List<string>()
        {
            "Apple", "Something something something", "Cherry is the best", "Banana is yellow"
        };

        var rnd = new Random();

        var currentFileSize = 0;

        using (FileStream fs = File.Create(FileName))
        {
            while (currentFileSize < size * 1048576) // Mb -> Bytes
            {
                using (var rg = RandomNumberGenerator.Create())
                {
                    byte[] rno = new byte[8];
                    rg.GetBytes(rno);
                    long randomvalue = BitConverter.ToInt64(rno, 0) < 0
                        ? BitConverter.ToInt64(rno, 0) * -1
                        : BitConverter.ToInt64(rno, 0);

                    int r = rnd.Next(dict.Count);
                    Byte[] str = new UTF8Encoding(true).GetBytes($"{randomvalue}.  {dict[r]}\n");
                    fs.Write(str, 0, str.Length);
                    currentFileSize += $"{randomvalue}.  {dict[r]}\n".Length * sizeof(Char) / 2;
                }
            }
        }
    }
}