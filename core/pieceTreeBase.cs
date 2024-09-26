using System.Collections;
using System.Reflection.Metadata;

namespace buffer;

//lint starts
public class LineStarts
{
    public ArrayList? lineStarts;
    public readonly int cr;
    public readonly int lf;
    public readonly int crlf;
    public readonly bool isBasicASCII;
    public LineStarts(ArrayList lineStarts, int cr, int lf, int crlf, bool isBasicASCII)
    {
        this.lineStarts = lineStarts;
        this.cr = cr;
        this.lf = lf;
        this.crlf = crlf;
        this.isBasicASCII = isBasicASCII;
    }
    public LineStarts() { }

    public ArrayList createLineStartsFast(string str, bool ReadOnly = true)
    {
        ArrayList list = new ArrayList() { 0 };
        int len = str.Length;
        for (int i = 0; i < len; i++)
        {
            var chr = str[i];
            if (chr == (int)CharCode.CarriageReturn)
            {
                if (i + 1 < len && str[i + 1] == (int)CharCode.LineFeed)
                {
                    //\r\n
                    list.Add(i + 2);
                    i++;
                }
                else
                {
                    //\r
                    list.Add(i + 1);
                }
            }
            else if (chr == (int)CharCode.LineFeed)
            {
                list.Add(i + 1);
            }
        }

        return list;
    }
    public LineStarts creteLineStarts(ArrayList arr, string str)
    {

        ArrayList list = new ArrayList();
        int cr = 0, lf = 0, crlf = 0;
        bool isBasicASCII = true;

        int len = str.Length;
        for (int i = 0; i < len; i++)
        {
            var chr = str[i];
            if (chr == (int)CharCode.CarriageReturn)
            {
                if (i + 1 < len && str[i + 1] == (int)CharCode.LineFeed)
                {
                    //\r\n
                    crlf++;
                    list.Add(i + 2);
                    i++; //skip \n
                }
                else
                {
                    //\r
                    cr++;
                    list.Add(i + 1);
                }
            }
            else if (chr == (int)CharCode.LineFeed)
            {
                lf++;
                list.Add(i + 1);
            }
            else
            {
                if (isBasicASCII)
                {
                    if (chr != (int)CharCode.Tab && (chr < 32 || chr > 126))
                    {
                        isBasicASCII = false;
                    }
                }
            }
        }

        LineStarts lineStarts = new LineStarts(list, cr, lf, crlf, isBasicASCII);
        return lineStarts;
    }
}

struct NodePosition
{
    TreeNode node;
    int remainder;
    int nodeStartOffset;
}

struct PieceIndex
{
    TreeNode node;
    int remainder;
    int nodeStartOffset;
}

struct CacheEntry
{
    TreeNode node { get; set; }
    int? nodeStartLineNumber { set; get; }
    int nodeStartOffset { set; get; }

    public CacheEntry(CacheEntry entry)
    {
        this.node = entry.node;
        this.nodeStartOffset = entry.nodeStartOffset;
        this.nodeStartLineNumber = entry.nodeStartLineNumber;
    }
}

struct BufferCursor
{
    int line;
    int column;
    public BufferCursor(int line, int column)
    {
        this.line = line;
        this.column = column;
    }
}

struct Piece
{
    readonly int bufferIndex;
    readonly BufferCursor start;
    readonly BufferCursor end;
    readonly int length;
    readonly int lineFeedCnt;

    public Piece(int bufferIndex, BufferCursor start, BufferCursor end, int length, int lineFeedCnt)
    {
        this.bufferIndex = bufferIndex;
        this.start = start;
        this.end = end;
        this.length = length;
        this.lineFeedCnt = lineFeedCnt;
    }
}

struct StringBuffer
{
    string buffer;
    ArrayList lineStarts;

    public StringBuffer(string buffer, ArrayList lineStarts)
    {
        this.buffer = buffer;
        this.lineStarts = lineStarts;
    }
}

public class PieceTreeSnapshot{
    private readonly List<Piece> _pieces;
    private int _index;
    private readonly PieceTreeBase _tree;
    private readonly string _BOM;

    PieceTreeSnapshot(PieceTreeBase tree, string BOM)
    {
        this._pieces = new List<Piece>();
        this._tree = tree;
        this._BOM = BOM;
        this._index = 0;
        if()

    }
    
}

public class PieceTreeBase{
    enum EOL {CRLF, LF};
    public TreeNode root = TreeNode.SENTIENL;
    List<StringBuffer> bufs;
    int _lineCnt;
    int length;
    EOL _EOL;
    int _EOLLength;
    int 

}