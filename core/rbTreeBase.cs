namespace piecetable;



public class TreeNode
{
    public TreeNode? parent;
    public TreeNode? left;
    public TreeNode? right;
    public NodeColor color;

    public Piece? piece;
    public int size_left;
    public int lf_left;

    public readonly static TreeNode SENTINEL = new TreeNode(new Piece(0, new BufferCursor(0, 0), new BufferCursor(0, 0), 0, 0), NodeColor.Black);

    public TreeNode(Piece piece, NodeColor color)
    {
        this.piece = piece;
        this.color = color;
        this.size_left = 0;
        this.lf_left = 0;
        this.parent = null;
        this.left = null;
        this.right = null;
        this.parent = this;
        this.left = this;
        this.right = this;
    }
    public TreeNode() { }



    public TreeNode next()
    {
        if (this.right != SENTINEL)
        {
            return leftest(this.right!);
        }

        var node = this;

        while (node.parent != SENTINEL)
        {
            if (node.parent!.left == node)
            {
                break;
            }

            node = node.parent;
        }

        if (node.parent == SENTINEL)
        {
            return SENTINEL;
        }
        else
        {
            return node.parent;
        }
    }

    public static TreeNode leftest(TreeNode node)
    {
        while (node!.left != SENTINEL)
        {
            node = node.left!;
        }
        return node;
    }

    public TreeNode prev()
    {
        if (this.left != SENTINEL)
        {
            return righttest(this.left!);
        }

        TreeNode node = this;
        while (node.parent != SENTINEL)
        {
            if (node.parent!.right == node)
            {
                break;
            }
            node = node.parent;
        }

        if (node.parent == SENTINEL)
        {
            return SENTINEL;
        }
        else
        {
            return node.parent;
        }
    }

    public static void updateTreeMetadata(PieceTreeBase tree, TreeNode x, int delta, int lineFeddCntDelta)
    {
        while(x != tree.root && x != SENTINEL)
        {
            if(x.parent!.left == x)
            {
                x.parent.size_left += delta;
                x.parent.lf_left += lineFeddCntDelta;
            }
            x = x.parent;
        }
    }

    


    public static int calculateSize(TreeNode node)
    {
        if (node == SENTINEL)
        {
            return 0;
        }
        return node.size_left + node.piece!.length + calculateSize(node.right!);
    }

    public static int calculateLF(TreeNode node)
    {
        if (node == SENTINEL)
        {
            return 0;
        }
        return node.lf_left + node.piece!.length + calculateLF(node.right!);
    }

    public static TreeNode righttest(TreeNode node)
    {
        while (node!.right != SENTINEL)
        {
            node = node.right!;
        }
        return node;
    }
}