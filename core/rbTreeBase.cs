namespace piecetable;


public class TreeNode{
    public TreeNode ?parent;
    public TreeNode ?left;
    public TreeNode ?right;
    public NodeColor color;

    public Piece piece;
    public int size_left;
    public int lf_left;

    public readonly TreeNode SENTINEL = new TreeNode(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);

    public TreeNode(Piece piece, NodeColor color)
    {
        this.piece = piece;
        this.color=color;
        this.size_left = 0;
        this.lf_left = 0;
        this.parent = null;
        this.left = null;
        this.right = null;
        this.parent = this;
        this.left = this;
        this.right = this;
    }
    public TreeNode(){}
}