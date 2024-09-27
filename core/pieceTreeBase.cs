using System.Collections;
using System.Dynamic;
using System.Reflection.Metadata;
using System.Xml.Schema;
using Microsoft.VisualBasic;

namespace piecetable;

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

public class NodePosition
{
    public TreeNode? node;
    public int remainder;
    public int nodeStartOffset;
}

public class PieceIndex
{
    TreeNode node;
    int remainder;
    int nodeStartOffset;
}

public class CacheEntry
{
    public TreeNode node { get; set; }
    public int? nodeStartLineNumber { set; get; }
    public int nodeStartOffset { set; get; }

    public CacheEntry(CacheEntry entry)
    {
        this.node = entry.node;
        this.nodeStartOffset = entry.nodeStartOffset;
        this.nodeStartLineNumber = entry.nodeStartLineNumber;
    }
}

public class BufferCursor
{
    public int line;
    public int column;
    public BufferCursor(int line, int column)
    {
        this.line = line;
        this.column = column;
    }
}

public class Piece
{
    public readonly int bufferIndex;
    public readonly BufferCursor start;
    public readonly BufferCursor end;
    public readonly int length;
    public readonly int lineFeedCnt;

    public Piece(int bufferIndex, BufferCursor start, BufferCursor end, int length, int lineFeedCnt)
    {
        this.bufferIndex = bufferIndex;
        this.start = start;
        this.end = end;
        this.length = length;
        this.lineFeedCnt = lineFeedCnt;
    }
}

public class StringBuffer
{
    public string buffer { set; get; }
    public ArrayList lineStarts { set; get; }

    public StringBuffer(string buffer, ArrayList lineStarts)
    {
        this.buffer = buffer;
        this.lineStarts = lineStarts;
    }
}


public class PieceTreeSearchCache
{
    private int _limt;
    private List<CacheEntry>? _cache;

    public PieceTreeSearchCache(int limit)
    {
        this._limt = limit;
        this._cache = new List<CacheEntry>();
    }

    public CacheEntry? get(int offset)
    {
        int len = this._cache.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var nodePos = this._cache[i];
            if (nodePos.nodeStartOffset <= offset && nodePos.nodeStartOffset + nodePos.node.piece.length >= offset)
            {
                return nodePos;
            }
        }
        return null;
    }

    public (TreeNode node, int nodeStartOffset, int? nodeStartLineNumber)? get2(int lineNumber)
    {
        var len = this._cache.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var nodePos = this._cache[i];
            if (nodePos.nodeStartLineNumber != null && nodePos.nodeStartLineNumber < lineNumber && nodePos.nodeStartLineNumber + nodePos.node.piece.lineFeedCnt >= lineNumber)
            {
                return (nodePos.node, nodePos.nodeStartOffset, nodePos.nodeStartLineNumber);
            }
        }
        return null;
    }

    public void set(CacheEntry nodePosition)
    {
        if (this._cache.Count() > this._limt)
        {
            this._cache.RemoveAt(0);
        }
        this._cache.Append(nodePosition);
    }

    public void validate(int offset)
    {
        bool hasInvalidVal = false;
        List<CacheEntry>? tmp = this._cache;
        int len = tmp.Count();
        for (int i = 0; i < len; i++)
        {
            var ndoePos = tmp[i];
            if (ndoePos.node.parent == null || ndoePos.nodeStartOffset >= offset)
            {
                tmp[i] = null;
                hasInvalidVal = true;
                continue;

            }
        }

        if (hasInvalidVal)
        {
            List<CacheEntry>? list = new List<CacheEntry>();
            foreach (CacheEntry entry in tmp)
            {
                if (entry.Equals(null))
                {
                    list.Append(entry);
                }
            }
            this._cache = list;
        }
    }

}

public class PieceTreeSnapshot
{
    private readonly List<Piece> _pieces;
    private int _index;
    private readonly PieceTreeBase _tree;
    private readonly string _BOM;

    private readonly TreeNode SENTINEL = new TreeNode(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);
    public PieceTreeSnapshot(PieceTreeBase tree, string BOM)
    {
        this._pieces = new List<Piece>();
        this._tree = tree;
        this._BOM = BOM;
        this._index = 0;
        if (tree.root != SENTINEL)
        {
            tree.iterate(tree.root, node =>
            {
                if (node != SENTINEL)
                {
                    _pieces.Append(node.piece);
                }
                return true;
            });
        }
    }

    public string? Read()
    {
        if (this._pieces.Count() == 0)
        {
            if (this._index == 0)
            {
                this._index++;
                return this._BOM;
            }
            else
            {
                return null;
            }
        }

        if (this._index > this._pieces.Count() - 1)
        {
            return null;
        }
        if (this._index == 0)
        {
            return this._BOM + this._tree.GetPieceContent(this._pieces[this._index++]);
        }
        return this._tree.GetPieceContent(this._pieces[this._index++]);
    }
}



public class PieceTreeBase
{
    public enum EOL { CRLF, LF };
    public TreeNode root;

    List<StringBuffer>? _buffers;
    int _lineCnt;
    int length;
    EOL _EOL;
    int _EOLLength;

    bool _EOLNoramlized;
    BufferCursor? _lastChangeBufferPos;
    (int lineNumber, string value) _lastVisitedLine;

    private TreeNode SENTINEL = new(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);


    public PieceTreeBase(List<StringBuffer> chunks, EOL eol, bool eolNormalized)
    {
        this._buffers = new List<StringBuffer>();
        this._lastChangeBufferPos = new BufferCursor(0, 0);
        this.root = SENTINEL;
        this._lineCnt = 1;
        this.length = 0;
        this._EOL = eol;
        this._EOLLength = getEOLString(eol).Length;
        this._EOLNoramlized = eolNormalized;

        TreeNode? lastNode = null;
        int len = chunks.Count();
        for (int i = 0; i < len; i++)
        {
            if (chunks[i].buffer.Length > 0)
            {
                if (chunks[i].lineStarts == null)
                {
                    chunks[i].lineStarts = new LineStarts().createLineStartsFast(chunks[i].buffer);
                }

                var piece = new Piece(i + 1, new BufferCursor(0, 0), new BufferCursor(chunks[i].lineStarts.Count - 1, chunks[i].buffer.Length - chunks[i].lineStarts.IndexOf(chunks[i].lineStarts.Count - 1)), chunks[i].lineStarts.Count - 1, chunks[i].buffer.Length);
                this._buffers.Add(chunks[i]);
                lastNode = this.rbInsertRight(lastNode, piece);
            }
        }


    }

    private TreeNode rbInsertRight(TreeNode node, Piece p)
    {
        var z = new TreeNode(p, NodeColor.Red);
        z.left = SENTINEL;
        z.right = SENTINEL;
        z.parent = SENTINEL;
        z.size_left = 0;
        z.lf_left = 0;

        var x = this.root;
        if (x == SENTINEL)
        {
            this.root = z;
            z.color = NodeColor.Black;
        }
        else if (node.right == SENTINEL)
        {
            node.right = z;
            z.parent = node;
        }
        else
        {
            var nextNode = leftest(node.right);
            nextNode.left = z;
            z.parent = nextNode;
        }

        fixInsert(this, z);
        return z;

    }
    public void fixInsert(PieceTreeBase tree, TreeNode x)
    {
        recomputeTreeMetadata(tree, x);

        while (x != tree.root && x.parent.color == NodeColor.Red)
        {
            if (x.parent == x.parent.parent.left)
            {
                var y = x.parent.parent.right;
                if (y.color == NodeColor.Red)
                {
                    x.parent.color = NodeColor.Black;
                    y.color = NodeColor.Black;
                    x.parent.parent.color = NodeColor.Red;
                    x = x.parent.parent;
                }
                else
                {
                    if (x == x.parent.right)
                    {
                        x = x.parent;
                        leftRotate(tree, x);
                    }

                    x.parent.color = NodeColor.Black;
                    x.parent.parent.color = NodeColor.Red;
                    rightRotate(tree, x.parent.parent);
                }
            }
            else
            {
                var y = x.parent.parent.left;

                if (y.color == NodeColor.Red)
                {
                    x.parent.color = NodeColor.Black;
                    y.color = NodeColor.Black;
                    x.parent.parent.color = NodeColor.Red;
                    x = x.parent.parent;
                }
                else
                {
                    if (x == x.parent.left)
                    {
                        x = x.parent;
                        rightRotate(tree, x);
                    }
                    x.parent.color = NodeColor.Black;
                    x.parent.parent.color = NodeColor.Red;
                    leftRotate(tree, x.parent.parent);
                }
            }

        }

        tree.root.color = NodeColor.Black;
    }

    public void leftRotate(PieceTreeBase tree, TreeNode x)
    {
        var y = x.right;

        // fix size_left
        if (x.piece != null)
        {
            y.size_left += x.size_left + x.piece.length;
            y.lf_left += x.lf_left + x.piece.lineFeedCnt;
        }
        else
        {
            y.size_left += x.size_left;
            y.lf_left += x.lf_left;
        }

        x.right = y.left;

        if (y.left != SENTINEL)
        {
            y.left.parent = x;
        }
        y.parent = x.parent;
        if (x.parent == SENTINEL)
        {
            tree.root = y;
        }
        else if (x.parent.left == x)
        {
            x.parent.left = y;
        }
        else
        {
            x.parent.right = y;
        }
        y.left = x;
        x.parent = y;
    }

    public void rightRotate(PieceTreeBase tree, TreeNode y)
    {
        var x = y.left;
        y.left = x.right;
        if (x.right != SENTINEL)
        {
            x.right.parent = y;
        }
        x.parent = y.parent;

        // fix size_left
        if (x.piece != null)
        {
            y.size_left += x.size_left + x.piece.length;
            y.lf_left += x.lf_left + x.piece.lineFeedCnt;
        }
        else
        {
            y.size_left += x.size_left;
            y.lf_left += x.lf_left;
        }

        if (y.parent == SENTINEL)
        {
            tree.root = x;
        }
        else if (y == y.parent.right)
        {
            y.parent.right = x;
        }
        else
        {
            y.parent.left = x;
        }

        x.right = y;
        y.parent = x;
    }



    public void recomputeTreeMetadata(PieceTreeBase tree, TreeNode x)
    {
        var delta = 0;
        var lf_delta = 0;
        if (x == tree.root)
        {
            return;
        }
        while (x != tree.root && x == x.parent.right)
        {
            x = x.parent;
        }

        if (x == tree.root)
        {
            return;
        }
        x = x.parent;

        delta = calculateSize(x.left) - x.size_left;
        lf_delta = calculateLF(x.left) - x.lf_left;

        x.size_left += delta;
        x.lf_left += lf_delta;

        //go upwards till root. O(LogN)
        while (x != tree.root && (delta != 0 || lf_delta != 0))
        {
            if (x.parent.left == x)
            {
                x.parent.size_left += delta;
                x.parent.lf_left += lf_delta;
            }
            x = x.parent;
        }
    }

    public int calculateSize(TreeNode node)
    {
        if (node == SENTINEL)
        {
            return 0;
        }
        return node.size_left + node.piece.length + calculateSize(node.right);
    }

    public int calculateLF(TreeNode node)
    {
        if (node == SENTINEL)
        {
            return 0;
        }
        return node.lf_left + node.piece.length + calculateLF(node.right);
    }

    public TreeNode leftest(TreeNode node)
    {
        while (node.left != SENTINEL)
        {
            node = node.left;
        }
        return node;
    }


    private string getEOLString(EOL eol)
    {
        if (eol == EOL.CRLF)
        {
            return "CRLF";
        }
        else if (eol == EOL.LF)
        {
            return "LF";
        }
        return null;
    }


    public bool iterate(TreeNode node, Func<TreeNode, bool> callback)
    {
        if (node == SENTINEL)
            return callback(SENTINEL);

        bool leftRet = iterate(node.left, callback);
        if (!leftRet)
            return leftRet;

        return callback(node) && iterate(node.right, callback);

    }

    public string GetPieceContent(Piece piece)
    {
        var buf = this._buffers[piece.bufferIndex];
        var startOffset = this.offsetInBuffer(piece.bufferIndex, piece.start);
        var endOffset = this.offsetInBuffer(piece.bufferIndex, piece.end);
        var currentContent = buf.buffer.Substring(startOffset, endOffset);

        return currentContent;
    }

    private int offsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        var lineStarts = this._buffers[bufferIndex].lineStarts;
        return lineStarts.IndexOf(cursor.line) + cursor.column;
    }

}