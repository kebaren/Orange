using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Xml.Schema;
using Microsoft.VisualBasic;

namespace piecetable;

//lint starts
public class LineStarts
{
    public List<int>? lineStarts;
    public readonly int cr;
    public readonly int lf;
    public readonly int crlf;
    public readonly bool isBasicASCII;
    public LineStarts(List<int> lineStarts, int cr, int lf, int crlf, bool isBasicASCII)
    {
        this.lineStarts = lineStarts;
        this.cr = cr;
        this.lf = lf;
        this.crlf = crlf;
        this.isBasicASCII = isBasicASCII;
    }
    public LineStarts() { }

    public static List<int> createLineStartsFast(string str, bool ReadOnly = true)
    {
        List<int> list = new List<int>() { 0 };
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
    public LineStarts creteLineStarts(List<int> arr, string str)
    {

        List<int> list = new List<int>();
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

    public NodePosition(TreeNode node, int remainder, int nodeStartOffset)
    {
        this.node = node;
        this.remainder = remainder;
        this.nodeStartOffset = nodeStartOffset;
    }
}

public class PieceIndex
{
    TreeNode? node;
    int remainder;
    int nodeStartOffset;
}

public class CacheEntry
{
    public TreeNode node { get; set; }
    public int? nodeStartLineNumber { set; get; }
    public int nodeStartOffset { set; get; }

    public CacheEntry(NodePosition node)
    {
        this.node = node.node!;
        this.nodeStartOffset = node.nodeStartOffset;
        this.nodeStartLineNumber = node.remainder;
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
    public List<int> lineStarts { set; get; }

    public StringBuffer(string buffer, List<int> lineStarts)
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
        int len = this._cache!.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var nodePos = this._cache[i];
            if (nodePos.nodeStartOffset <= offset && nodePos.nodeStartOffset + nodePos.node.piece!.length >= offset)
            {
                return nodePos;
            }
        }
        return null;
    }

    public (TreeNode node, int nodeStartOffset, int? nodeStartLineNumber)? get2(int lineNumber)
    {
        var len = this._cache!.Count;
        for (int i = len - 1; i >= 0; i--)
        {
            var nodePos = this._cache[i];
            if (nodePos.nodeStartLineNumber != null && nodePos.nodeStartLineNumber < lineNumber && nodePos.nodeStartLineNumber + nodePos.node.piece!.lineFeedCnt >= lineNumber)
            {
                return (nodePos.node, nodePos.nodeStartOffset, nodePos.nodeStartLineNumber);
            }
        }
        return null;
    }

    public void set(CacheEntry nodePosition)
    {
        if (this._cache!.Count() > this._limt)
        {
            this._cache!.RemoveAt(0);
        }
        this._cache!.Append(nodePosition);
    }

    public void validate(int offset)
    {
        bool hasInvalidVal = false;
        List<CacheEntry>? tmp = this._cache;
        int len = tmp!.Count();
        for (int i = 0; i < len; i++)
        {
            var ndoePos = tmp![i];
            if (ndoePos.node.parent == null || ndoePos.nodeStartOffset >= offset)
            {
                tmp[i] = null!;
                hasInvalidVal = true;
                continue;

            }
        }

        if (hasInvalidVal)
        {
            List<CacheEntry>? list = new List<CacheEntry>();
            foreach (CacheEntry entry in tmp!)
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
    public required TreeNode root;

    List<StringBuffer>? _buffers;
    int _lineCnt { set; get; }
    int _length { set; get; }

    EOL _EOL;
    int _EOLLength;

    bool _EOLNoramlized;
    BufferCursor? _lastChangeBufferPos;
    public required PieceTreeSearchCache _searchCache;
    (int lineNumber, string value) _lastVisitedLine;

    private TreeNode SENTINEL = new(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);


    public PieceTreeBase(List<StringBuffer> chunks, EOL eol, bool eolNormalized)
    {
        this.crate(chunks, eol, eolNormalized);
    }

    public void crate(List<StringBuffer> chunks, EOL eol, bool eolNormalized)
    {
        this._buffers = new List<StringBuffer>();
        this._lastChangeBufferPos = new BufferCursor(0, 0);
        this.root = SENTINEL;
        this._lineCnt = 1;
        this._length = 0;
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
                    chunks[i].lineStarts = LineStarts.createLineStartsFast(chunks[i].buffer);
                }

                var piece = new Piece(i + 1, new BufferCursor(0, 0), new BufferCursor(chunks[i].lineStarts.Count - 1, chunks[i].buffer.Length - chunks[i].lineStarts.IndexOf(chunks[i].lineStarts.Count - 1)), chunks[i].lineStarts.Count - 1, chunks[i].buffer.Length);
                this._buffers.Add(chunks[i]);
                lastNode = this.rbInsertRight(lastNode!, piece);
            }
        }

        this._searchCache = new PieceTreeSearchCache(1);
        this._lastVisitedLine = (1, "");
        computeBufferMetadata();
    }


    public void normalizeEOL(EOL eol)
    {
        var eolStr = "";
        if (eol == EOL.LF) eolStr = "\n";
        else eolStr = "\r\n";
        var averageBufferSize = 65535;
        var min = averageBufferSize - Math.Floor(Convert.ToDouble(averageBufferSize / 3));
        var max = min * 2;

        var tempChunk = "";
        var tempChunkLen = 0;

        var chunks = new List<StringBuffer>();

        this.iterate(this.root, node =>
        {
            var str = this.getNodeContent(node);
            var len = str.Length;
            if (tempChunkLen <= min || tempChunkLen + len < max)
            {
                tempChunk += str;
                tempChunkLen += len;
                return true;
            }

            String text;
            //flush anyways
            text = tempChunk.Replace("\r\n", eolStr).Replace("\r", eolStr).Replace("\n", eolStr);
            chunks.Append(new StringBuffer(text, LineStarts.createLineStartsFast(text)));
            tempChunk = str;
            tempChunkLen = len;
            return true;
        });

        if (tempChunkLen > 0)
        {
            var text = tempChunk.Replace("\r\n", eolStr).Replace("\r", eolStr).Replace("\n", eolStr);
            chunks.Append(new StringBuffer(text, LineStarts.createLineStartsFast(text)));

        }
        this.crate(chunks, eol, true);
    }

    public string getEOL()
    {
        if (this._EOL == EOL.LF) return "\n";
        else return "\r\n";
    }

    public void setEOL(EOL eol)
    {
        this._EOL = eol;
        this._EOLLength = this.getEOL().Length;
        this.normalizeEOL(eol);
    }

    public PieceTreeSnapshot creatSnapshot(String BOM)
    {
        return new PieceTreeSnapshot(this, BOM);
    }

    public bool equal(PieceTreeBase other)
    {
        if (this._length != other._length)
        {
            return false;
        }

        if (this._lineCnt != other._lineCnt)
        {
            return false;
        }

        var offset = 0;
        var ret = this.iterate(this.root, node =>
        {
            if (node == SENTINEL)
            {
                return true;
            }

            var str = this.getNodeContent(node);
            var len = str.Length;
            var startPosition = other.nodeAt(offset);
            var endPosition = other.nodeAt(offset + len);
            var val = other.getValueInRange2(startPosition!, endPosition!);

            offset += len;
            return str == val;
        });

        return ret;

    }

    NodePosition? nodeAt(int offset)
    {
        var x = this.root;
        var cache = this._searchCache.get(offset);
        if (cache != null)
        {
            return new NodePosition(cache.node, cache.nodeStartOffset, offset - cache.nodeStartOffset);
        }

        var nodeStartOffset = 0;
        while (x != SENTINEL)
        {
            if (x!.size_left > offset)
            {
                x = x.left;

            }
            else if (x.size_left + x.piece!.length >= offset)
            {
                nodeStartOffset += x.size_left;
                var ret = new NodePosition(x, offset - x.size_left, nodeStartOffset);
                this._searchCache.set(new CacheEntry(ret));
                return ret;
            }
        }

        return null;
    }

    NodePosition nodeAt2(int lineNumber, int column)
    {
        var x = this.root;
        var nodeStartOffset = 0;
        var accumulatedValue = 0;
        var prevAccumualtedValue = 0;

        while (x != SENTINEL)
        {
            if (x!.left != SENTINEL && x.lf_left >= lineNumber - 1)
            {
                x = x.left;
            }
            else if (x.lf_left + x.piece!.lineFeedCnt > lineNumber - 1)
            {
                prevAccumualtedValue = this.getAccumulatedValue(x, lineNumber - x.lf_left - 2);
                accumulatedValue = this.getAccumulatedValue(x, lineNumber - x.lf_left - 1);
                nodeStartOffset += x.size_left;

                return new NodePosition(x, Math.Min(prevAccumualtedValue + column - 1, accumulatedValue), nodeStartOffset);
            }
            else if (x.lf_left + x.piece.lineFeedCnt == lineNumber - 1)
            {
                prevAccumualtedValue = this.getAccumulatedValue(x, lineNumber - x.lf_left - 2);
                if (prevAccumualtedValue + column - 1 <= x.piece.length)
                {
                    return new NodePosition(x, prevAccumualtedValue + column - 1, nodeStartOffset);
                }
                else
                {
                    column -= x.piece.length - prevAccumualtedValue;
                    break;
                }
            }
            else
            {
                lineNumber -= x.lf_left + x.piece.lineFeedCnt;
                nodeStartOffset += x.size_left + x.piece.length;
                x = x.right;
            }
        }
        x = x.next();
        while (x != SENTINEL)
        {

            if (x.piece!.lineFeedCnt > 0)
            {
                accumulatedValue = this.getAccumulatedValue(x, 0);
                nodeStartOffset = this.offsetOfNode(x);
                return new NodePosition(x, Math.Min(column - 1, accumulatedValue), nodeStartOffset);
            }
            else
            {
                if (x.piece.length >= column - 1)
                {
                    nodeStartOffset = this.offsetOfNode(x);
                    return new NodePosition(x, column - 1, nodeStartOffset);
                }
                else
                {
                    column -= x.piece.length;
                }
            }

            x = x.next();
        }

        return null!;
    }

    private int nodeCharCodeAt(TreeNode node, int offset)
    {
        if (node.piece!.lineFeedCnt < 1)
        {
            return -1;
        }
        var buffer = this._buffers![node.piece.bufferIndex];
        var newOffset = this.offsetInBuffer(node.piece.bufferIndex, node.piece.start) + offset;
        return char.ConvertToUtf32(buffer.buffer, newOffset);
    }

    private int offsetOfNode(TreeNode node)
    {
        if (node == null)
        {
            return 0;
        }
        var pos = node.size_left;
        while (node != this.root!)
        {
            if (node.parent!.right == node)
            {
                pos += node.parent.size_left + node.parent.piece!.length;
            }

            node = node.parent;
        }

        return pos;
    }

    private bool shouldCheckCRLF()
    {
        return !(this._EOLNoramlized && this._EOL == EOL.LF);
    }

    private bool startWithLF(Object val)
    {
        if (val is string str)
        {
            return str.Length > 0 && (int)str[0] == 10;
        }
        if (val is TreeNode node)
        {
            if (node == SENTINEL || node.piece!.lineFeedCnt == 0)
            {
                return false;
            }
            Piece piece = node.piece;
            StringBuffer buffer = _buffers![piece.bufferIndex];
            int line = piece.start.line;
            int startOffset = buffer.lineStarts[line] + piece.start.column;
            if (line == buffer.lineStarts.Count() - 1)
            {
                // last line, so there is no line feed at the end of this line
                return false;
            }
            int nextLineOffset = buffer.lineStarts[line + 1];
            if (nextLineOffset > startOffset + 1)
            {
                return false;
            }
            return (int)buffer.buffer[startOffset] == 10;
        }
        return false;
    }

    private bool endWithCR(object val)
    {
        if (val is string str)
        {
            return str.Length > 0 && (int)str[str.Length - 1] == 13;
        }
        if (val is TreeNode node)
        {
            if (node == SENTINEL || node.piece!.lineFeedCnt == 0)
            {
                return false;
            }
            return NodeCharCodeAt(node, node.piece.length - 1) == 13;
        }
        return false;
    }

    private void ValidateCRLFWithPrevNode(TreeNode nextNode)
    {
        if (shouldCheckCRLF() && startWithLF(nextNode))
        {
            TreeNode node = nextNode.prev();
            if (endWithCR(node))
            {
                fixCRLF(node, nextNode);
            }
        }
    }

    private void validateCRLFWithNextNode(TreeNode node)
    {
        if (shouldCheckCRLF() && endWithCR(node))
        {
            TreeNode nextNode = node.next();
            if (startWithLF(nextNode))
            {
                fixCRLF(node, nextNode);
            }
        }
    }

    private void fixCRLF(TreeNode prev, TreeNode next)
    {
        var nodesToDel = new List<TreeNode>();
        var lineStarts = this._buffers![prev.piece!.bufferIndex].lineStarts;
        BufferCursor newEnd;

        if(prev.piece!.end.column == 0)
        {
            newEnd = new BufferCursor(prev.piece.end.line-1, lineStarts[prev.piece.end.line]-lineStarts[prev.piece.end.line-1]-1);
        }else{
            newEnd = new BufferCursor(prev.piece.end.line, prev.piece.end.column-1);
        }

        var preVnewLength = prev.piece.length-1;
        var prevNewLFCnt = prev.piece.lineFeedCnt-1;
        prev.piece = new Piece(prev.piece.bufferIndex,prev.piece.start,newEnd,prevNewLFCnt,preVnewLength);

        TreeNode.updateTreeMetadata(this,prev,-1,-1);
        if(prev.piece.length == 0)
        {
            nodesToDel.Append(prev);
        }
        
    }

    private bool adjustCarriageReturnFromNext(string value, TreeNode node)
    {
        if (shouldCheckCRLF() && endWithCR(value))
        {
            TreeNode nextNode = node.next();
            if (startWithLF(nextNode))
            {
                // move `\n` forward
                value += '\n';


                if (nextNode.piece?.length == 1)
                {
                    node.rbDelete(this, nextNode);
                }
                else
                {
                    Piece piece = nextNode.piece!;
                    BufferCursor newStart = new BufferCursor(piece.start.line+1,0);
                    int newLength = piece.length - 1;
                    int newLineFeedCnt = getLineFeedCnt(piece.bufferIndex, newStart, piece.end);
                    nextNode.piece = new Piece
                    (
                        piece.bufferIndex,
                        newStart,
                        piece.end,
                        newLineFeedCnt,
                        newLength
                    );


                    TreeNode.updateTreeMetadata(this, nextNode, -1, -1);
                }
                return true;
            }
        }


        return false;
    }

    private int getLineFeedCnt(int bufferIndex, BufferCursor start, BufferCursor end)
    {
        if(end.column == 0) return end.line-start.line;

        var lineStarts = this._buffers![bufferIndex].lineStarts;
        if(end.line == lineStarts.Count()-1) return end.line- start.line;

        var nextLineStartOffset = lineStarts[end.line+1];
        var endOffset = lineStarts[end.line]+end.column;

        if(nextLineStartOffset>endOffset+1) return end.line - start.line;

        var previousCharOffset = endOffset-1;
        var buffer = this._buffers![bufferIndex].buffer;

        if(buffer[previousCharOffset] == 13)
        {
            return end.line-start.line+1;
        }else {
            return end.line - start.line;
        }

    }

    private int NodeCharCodeAt(TreeNode node, int offset)
    {
        if (node.piece!.lineFeedCnt < 1)
        {
            return -1;
        }
        StringBuffer buffer = this._buffers![node.piece.bufferIndex];
        int newOffset = OffsetInBuffer(node.piece.bufferIndex, node.piece.start) + offset;
        return (int)buffer.buffer[newOffset];
    }

    private int OffsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        var lineStarts = this._buffers![bufferIndex].lineStarts;
        return lineStarts[cursor.line] + cursor.column;
    }




    public int getAccumulatedValue(TreeNode node, int index)
    {
        if (index < 0)
        {
            return 0;
        }
        var piece = node.piece;
        var lineStarts = this._buffers![piece!.bufferIndex].lineStarts;
        var expectedLineStartIndex = piece.start.line + index + 1;
        if (expectedLineStartIndex > piece.end.line)
        {
            return lineStarts[piece.end.line] + piece.end.column - lineStarts[piece.start.line] - piece.start.column;
        }
        else
        {
            return lineStarts[expectedLineStartIndex] - lineStarts[piece.start.line] - piece.start.column;
        }
    }

    public string getValueInRange2(NodePosition startPosition, NodePosition endPosition)
    {
        string buffer = "";
        int startOffset;


        if (startPosition.node == endPosition.node)
        {
            var node = startPosition.node;
            buffer = this._buffers![node!.piece!.bufferIndex].buffer;
            startOffset = this.offsetInBuffer(node.piece.bufferIndex, node.piece.start);
            return buffer.Substring(startOffset + startPosition.remainder, endPosition.remainder);
        }

        var x = startPosition.node;
        buffer = this._buffers![x!.piece!.bufferIndex].buffer;
        startOffset = this.offsetInBuffer(x.piece.bufferIndex, x.piece.start);
        var ret = buffer.Substring(startOffset + startPosition.remainder, x.piece.length);

        x = x.next();
        while (x != SENTINEL)
        {
            buffer = this._buffers[x.piece!.bufferIndex].buffer;
            startOffset = this.offsetInBuffer(x.piece.bufferIndex, x.piece.start);

            if (x == endPosition.node)
            {
                ret += buffer.Substring(startOffset, endPosition.remainder);
                break;
            }
            else
            {
                ret += buffer.Substring(startOffset, x.piece.length);
            }

            x = x.next();
        }

        return ret;




    }

    private String getNodeContent(TreeNode node)
    {
        if (node == SENTINEL) return "";

        var buffer = this._buffers![node.piece!.bufferIndex];
        var piece = node.piece;
        var startOffset = this.offsetInBuffer(piece.bufferIndex, piece.start);
        var endOffset = this.offsetInBuffer(piece.bufferIndex, piece.end);
        return buffer.buffer.Substring(startOffset, endOffset);
    }

    void computeBufferMetadata()
    {
        var x = this.root;
        var lfCnt = 1;
        var len = 0;

        while (x != SENTINEL)
        {
            lfCnt += x!.lf_left + x.piece!.lineFeedCnt;
            len += x.size_left + x.piece.length;
            x = x.right;
        }

        this._lineCnt = lfCnt;
        this._length = len;
        this._searchCache.validate(this._length);
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
            var nextNode = TreeNode.leftest(node.right!);
            nextNode.left = z;
            z.parent = nextNode;
        }

        fixInsert(this, z);
        return z;

    }
    public void fixInsert(PieceTreeBase tree, TreeNode x)
    {
        recomputeTreeMetadata(tree, x);

        while (x != tree.root && x.parent!.color == NodeColor.Red)
        {
            if (x.parent == x.parent.parent!.left)
            {
                var y = x.parent.parent.right;
                if (y!.color == NodeColor.Red)
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
                        TreeNode.leftRotate(tree, x);
                    }

                    x.parent!.color = NodeColor.Black;
                    x.parent.parent!.color = NodeColor.Red;
                    TreeNode.rightRotate(tree, x.parent.parent);
                }
            }
            else
            {
                var y = x.parent.parent.left;

                if (y!.color == NodeColor.Red)
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
                        TreeNode.rightRotate(tree, x);
                    }
                    x.parent!.color = NodeColor.Black;
                    x.parent.parent!.color = NodeColor.Red;
                    TreeNode.leftRotate(tree, x.parent.parent);
                }
            }

        }

        tree.root.color = NodeColor.Black;
    }


    

    public void recomputeTreeMetadata(PieceTreeBase tree, TreeNode x)
    {
        var delta = 0;
        var lf_delta = 0;
        if (x == tree.root)
        {
            return;
        }
        while (x != tree.root && x == x.parent!.right)
        {
            x = x.parent;
        }

        if (x == tree.root)
        {
            return;
        }
        x = x.parent!;

        delta = TreeNode.calculateSize(x.left!) - x.size_left;
        lf_delta = TreeNode.calculateLF(x.left!) - x.lf_left;

        x.size_left += delta;
        x.lf_left += lf_delta;

        //go upwards till root. O(LogN)
        while (x != tree.root && (delta != 0 || lf_delta != 0))
        {
            if (x.parent!.left == x)
            {
                x.parent.size_left += delta;
                x.parent.lf_left += lf_delta;
            }
            x = x.parent;
        }
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
        return null!;
    }


    public bool iterate(TreeNode node, Func<TreeNode, bool> callback)
    {
        if (node == SENTINEL)
            return callback(SENTINEL);

        bool leftRet = iterate(node.left!, callback);
        if (!leftRet)
            return leftRet;

        return callback(node) && iterate(node.right!, callback);

    }

    public string GetPieceContent(Piece piece)
    {
        var buf = this._buffers![piece.bufferIndex];
        var startOffset = this.offsetInBuffer(piece.bufferIndex, piece.start);
        var endOffset = this.offsetInBuffer(piece.bufferIndex, piece.end);
        var currentContent = buf.buffer.Substring(startOffset, endOffset);

        return currentContent;
    }

    private int offsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        var lineStarts = this._buffers![bufferIndex].lineStarts;
        return lineStarts.IndexOf(cursor.line) + cursor.column;
    }

    private string getContetnOfSubTree(TreeNode node)
    {
        var str = "";

        this.iterate(node,node=>{
            str += this.getNodeContent(node);
            return true;
        });

        return str;
    }

}
