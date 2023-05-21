using System.IO;

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