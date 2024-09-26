namespace buffer;

public class TreeNode{
    TreeNode ?parent;
    TreeNode ?left;
    TreeNode ?right;
    NodeColor color;

    Piece piece;
    int size_left;
    int size_right;

    public const TreeNode SENTINEL = new TreeNode(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);

    TreeNode(Piece piece, NodeColor color)
    {
        this.piece = piece;
        this.color=color;
        this.size_left = 0;
        this.size_right = 0;
        this.parent = null;
        this.left = null;
        this.right = null;
        this.parent = this;
        this.left = this;
        this.right = this;
    }
}