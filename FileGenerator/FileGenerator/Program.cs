using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class FileGenerator
{
    private const string FileName = @"test.txt";

    public static void Main(string[] args)
    {
        GenerateFile();
    }

    private static void GenerateFile()
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

        var dict = new List<string>()
        {
            "Apple", "Something something something", "Cherry is the best", "Banana is yellow"
        };

        var rnd = new Random();

        var currentFileSize = 0;

        using var fs = File.Create(FileName);
        while (currentFileSize < size * 1048576) // Mb -> Bytes
        {
            using var rg = RandomNumberGenerator.Create();
            byte[] rno = new byte[8];
            rg.GetBytes(rno);
            var randomvalue = BitConverter.ToInt32(rno, 0) < 0
                ? BitConverter.ToInt32(rno, 0) * -1
                : BitConverter.ToInt32(rno, 0);

            var r = rnd.Next(dict.Count);
            var str = new UTF8Encoding(true).GetBytes($"{randomvalue}.  {dict[r]}\r\n");
            fs.Write(str, 0, str.Length);
            currentFileSize += $"{randomvalue}.  {dict[r]}\r\n".Length * sizeof(char) / 2;
        }
    }
}