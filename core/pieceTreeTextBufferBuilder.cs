using Microsoft.VisualBasic;

namespace piecetable;

public class PieceTreeTextBufferFactory{

    private readonly List<StringBuffer> _chunks;
    private readonly string _bom;
    private readonly int _cr;
    private readonly int _lf;
    private readonly int _crlf;
    private readonly bool _containsUnusualLineTerminators;
    private readonly bool _isBasicASCI;
    private readonly bool _normalizeEOL;

    public PieceTreeTextBufferFactory(List<StringBuffer> _chunks, string _bom, int cr, int lf, int crlf, bool _containsUnusualLineTerminators, bool _isBasicASCI, bool _normalizeEOL )
    {
        this._chunks = _chunks;
        this._bom = _bom;
        this._cr = cr;
        this._lf = lf;
        this._crlf = crlf;
        this._containsUnusualLineTerminators =  _containsUnusualLineTerminators;
        this._isBasicASCI = _isBasicASCI;
        this._normalizeEOL = _normalizeEOL;
    }

    private int[] getEOL(DefaultEndOfLine defaultEOL)
    {
        var totalEOLCount = this._cr+this._lf+this._crlf;
        var totalCRCount = this._cr+this._crlf;

        if(totalEOLCount == 0)
        {
            return (defaultEOL == DefaultEndOfLine.LF ? [10]: [13,10]);
        }

        if(totalCRCount > totalEOLCount/2)
        {
            return [13,10];
        }

        return [10];
    }
    public PieceTreeBase cretePieceTreeBase(DefaultEndOfLine defaultEOL = DefaultEndOfLine.LF)
    {
        var EOL = getEOL(defaultEOL);
        var chunks = this._chunks;
        if (_normalizeEOL && ((EOL == [13,10] && (cr>0 || lf>0)) || (EOL == [10] && cr>0 || _crlf >0))))
        {
            
        }
    }
}


public class PieceTextBufferBuilder
{
    private readonly List<StringBuffer> chunks;
    private string BOM;
    private bool _hasPreviousChar;
    private int _previousChar;
    private List<int> _tmpLineStarts;
    private int cr;
    private int lf;
    private int crlf;
    private bool containsRTL;
    private bool _containsUnusualLineTerminators;
    private bool isBasicASCII;

    public PieceTextBufferBuilder()
    {
        this.chunks = new List<StringBuffer>();
        this.BOM = "";
        this.crlf = 0;
        this.lf = 0;
        this.cr = 0;
        this._hasPreviousChar = false;
        this._previousChar  = 0;
        this._tmpLineStarts = new List<int>();
        this.containsRTL = false;
        this._containsUnusualLineTerminators = false;
        this.isBasicASCII = true;                       
    }

    public void accpetChunk(string chunk)
    {
        if(chunk.Length == 0)
        {
            return ;
        }

        if(this.chunks.Count() == 0)
        {
            if(Utils.startsWithUTF8BOM(chunk))
            {

            }
        }
    }