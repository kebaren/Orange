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
        while (x != tree.root && x != SENTINEL)
        {
            if (x.parent!.left == x)
            {
                x.parent.size_left += delta;
                x.parent.lf_left += lineFeddCntDelta;
            }
            x = x.parent;
        }
    }

    public void rbDelete(PieceTreeBase tree, TreeNode z)
    {
        TreeNode x;
        TreeNode y;

        if (z.left == null)
        {
            y = z;
            x = y.right!;
        }
        else if (z.right == null)
        {
            y = z;
            x = y.left;
        }
        else
        {
            y = leftest(z.right);
            x = y.right!;
        }

        if (y == tree.root)
        {
            tree.root = x;
            // 如果x为null，我们正在移除唯一的节点
            if (x != null)
            {
                x.color = NodeColor.Black;
            }
            z.detach();
            resetSentinel();
            if (tree.root != null)
            {
                tree.root.parent = null;
            }
            return;
        }

        bool yWasRed = (y.color == NodeColor.Red);

        if (y == y.parent!.left)
        {
            y.parent.left = x;
        }
        else
        {
            y.parent.right = x;
        }

        if (y == z)
        {
            x.parent = y.parent;
            this.recomputeTreeMetadata(tree, x);
        }
        else
        {
            if (y.parent == z)
            {
                x.parent = y;
            }
            else
            {
                x.parent = y.parent;
            }
            recomputeTreeMetadata(tree, x);

            y.left = z.left;
            y.right = z.right;
            y.parent = z.parent;
            y.color = z.color;

            if (z == tree.root)
            {
                tree.root = y;
            }
            else
            {
                if (z == z.parent!.left)
                {
                    z.parent.left = y;
                }
                else
                {
                    z.parent.right = y;
                }
            }

            if (y.left != null)
            {
                y.left.parent = y;
            }
            if (y.right != null)
            {
                y.right.parent = y;
            }
            // 更新元数据
            // 我们用y替换z，所以在这个子树中，长度变化是z.Data的长度（假设Data是相关数据属性）
            y.size_left = z.size_left;
            y.lf_left = z.lf_left;
            recomputeTreeMetadata(tree, y);
        }

        z.detach();

        if (x.parent.left == x)
        {
            int newSizeleft = calculateSize(x);
            int newLFleft = calculateLF(x);
            if (newSizeleft != x.parent.size_left || newLFleft != x.parent.lf_left)
            {
                int delta = newSizeleft - x.parent.size_left;
                int lf_delta = newLFleft - x.parent.lf_left;
                x.parent.size_left = newSizeleft;
                x.parent.lf_left = newLFleft;
                updateTreeMetadata(tree, x.parent, delta, lf_delta);
            }
        }

        recomputeTreeMetadata(tree, x.parent);

        if (yWasRed)
        {
            resetSentinel();
            return;
        }

        // RB - DELETE - FIXUP
        TreeNode w;
        while (x != tree.root && x.color == NodeColor.Black)
        {
            if (x == x.parent!.left)
            {
                w = x.parent.right!;

                if (w.color == NodeColor.Red)
                {
                    w.color = NodeColor.Black;
                    x.parent.color = NodeColor.Red;
                    leftRotate(tree, x.parent);
                    w = x.parent.right!;
                }

                if (w.left!.color == NodeColor.Black && w.right!.color == NodeColor.Black)
                {
                    w.color = NodeColor.Red;
                    x = x.parent;
                }
                else
                {
                    if (w.right!.color == NodeColor.Black)
                    {
                        w.left.color = NodeColor.Black;
                        w.color = NodeColor.Red;
                        rightRotate(tree, w);
                        w = x.parent.right!;
                    }

                    w.color = x.parent.color;
                    x.parent.color = NodeColor.Black;
                    w.right!.color = NodeColor.Black;
                    leftRotate(tree, x.parent);
                    x = tree.root;
                }
            }
            else
            {
                w = x.parent.left!;

                if (w.color == NodeColor.Red)
                {
                    w.color = NodeColor.Black;
                    x.parent.color = NodeColor.Red;
                    rightRotate(tree, x.parent);
                    w = x.parent.left!;
                }

                if (w.left!.color == NodeColor.Black && w.right!.color == NodeColor.Black)
                {
                    w.color = NodeColor.Red;
                    x = x.parent;
                }
                else
                {
                    if (w.left.color == NodeColor.Black)
                    {
                        w.right!.color = NodeColor.Black;
                        w.color = NodeColor.Red;
                        leftRotate(tree, w);
                        w = x.parent.left!;
                    }

                    w.color = x.parent.color;
                    x.parent.color = NodeColor.Black;
                    w.left!.color = NodeColor.Black;
                    rightRotate(tree, x.parent);
                    x = tree.root;
                }
            }
        }
        x.color = NodeColor.Black;
        resetSentinel();
    }

    public void detach()
    {
        this.parent = null!;
        this.left = null!;
        this.right = null!;
    }

    public void resetSentinel()
    {
        SENTINEL.parent = SENTINEL;
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

     public void recomputeTreeMetadata(PieceTreeBase tree, TreeNode x)
    {
        int delta = 0;
        int lf_delta = 0;
        if (x == tree.root)
        {
            return;
        }


        // go upwards till the node whose left subtree is changed.
        while (x!= tree.root && x == x.parent!.right)
        {
            x = x.parent;
        }


        if (x == tree.root)
        {
            // well, it means we add a node to the end (inorder)
            return;
        }


        // x is the node whose right subtree is changed.
        x = x.parent!;


        delta = calculateSize(x.left!) - x.size_left;
        lf_delta = calculateLF(x.left!) - x.lf_left;
        x.size_left += delta;
        x.lf_left += lf_delta;


        // go upwards till root. O(logN)
        while (x!= tree.root && (delta!= 0 || lf_delta!= 0))
        {
            if (x.parent!.left == x)
            {
                x.parent.size_left += delta;
                x.parent.lf_left += lf_delta;
            }


            x = x.parent;
        }
    }


    public static void leftRotate(PieceTreeBase tree, TreeNode x)
    {
        var y = x.right;

        // fix size_left
        if (x.piece != null)
        {
            y!.size_left += x.size_left + x.piece.length;
            y.lf_left += x.lf_left + x.piece.lineFeedCnt;
        }
        else
        {
            y!.size_left += x.size_left;
            y.lf_left += x.lf_left;
        }

        x.right = y.left;

        if (y.left != SENTINEL)
        {
            y.left!.parent = x;
        }
        y.parent = x.parent;
        if (x.parent == SENTINEL)
        {
            tree.root = y;
        }
        else if (x.parent!.left == x)
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

    public static void rightRotate(PieceTreeBase tree, TreeNode y)
    {
        var x = y.left;
        y.left = x!.right;
        if (x.right != SENTINEL)
        {
            x.right!.parent = y;
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
        else if (y == y.parent!.right)
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


}