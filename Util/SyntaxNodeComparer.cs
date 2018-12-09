using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Prem.Util
{
    /// <summary>
    /// Compare two syntax trees of type `SyntaxNode`,
    /// finding an edit which could transform the one to the other.
    /// </summary>
    public static class SyntaxNodeComparer
    {
        private static Logger Log = Logger.Instance;

        /// <summary>
        /// Compare two syntax trees and generate a `Result` indicating the edit which
        /// transform `oldNode` to `newNode`.
        /// </summary>
        /// <param name="oldNode">The old node.</param>
        /// <param name="newNode">The new node.</param>
        /// <returns>A transformation result.</returns>
        public static Result Compare(SyntaxNode oldNode, SyntaxNode newNode)
        {
            if (oldNode.kind != newNode.kind || !oldNode.label.Equals(newNode.label))
            {
                // These two nodes are totally different.
                return new Update(oldNode, newNode);
            }

            if (oldNode.kind == SyntaxKind.TOKEN) // If both are leaves...
            {
                if (oldNode.code == newNode.code) // they are identical if the values are the same,
                {
                    Log.Fine("Identical: {0} <-> {1}", oldNode, newNode);
                    return new Identical(oldNode);
                }

                // or, they are in fact different.
                return new Update(oldNode, newNode);
            }

            // Both must be nodes.
            var oldChildren = ((Node)oldNode).children;
            var newChildren = ((Node)newNode).children;

            var k1 = oldChildren.Count;
            var k2 = newChildren.Count;

            if (k1 == k2) // They have the same number of children, then compare them accordingly.
            {
                var resultsCache = new List<Result>();
                for (int i = 0; i < k1; i++)
                {
                    var oldChild = oldChildren[i];
                    var newChild = newChildren[i];
                    var result = Compare(oldChild, newChild);

                    if (result.kind != ResultKind.IDENTICAL)
                    {
                        // Temporarily cache the edit and make the decision in the end.
                        resultsCache.Add(result);
                    }
                    // else: So far so good, we will continue comparing the next ones.
                }

                if (!resultsCache.Any()) // No changes at all, these two nodes are identical.
                {
                    Log.Fine("Identical: {0} <-> {1}", oldNode, newNode);
                    // TODO: mark them as fully matched
                    return new Identical(oldNode);
                }

                if (resultsCache.Count == 1) // Only one edit is required.
                {
                    return resultsCache[0];
                }

                // Multiple edits are required.
                // Why not lift them up as a single update from `oldNode` to `newNode`?
                return new Update(oldNode, newNode);
            }

            if (k1 - k2 == 1) // One of the children is deleted.
            {
                // Then, which one? In the worst case, we have to attempt them all.
                // However, we can use an heuristic order.
                foreach (int j in HeuristicOrder(newChildren, oldChildren))
                {
                    var child = oldChildren[j];
                    Log.Fine("Guess: delete {0}", child);

                    // Let's see if `child` is deleted.
                    var hasEdit = false;
                    for (int i = 0; i < k2; i++)
                    {
                        var oldChild = oldChildren[i < j ? i : i + 1];
                        var newChild = newChildren[i];
                        var result = Compare(oldChild, newChild);

                        if (result.kind != ResultKind.IDENTICAL)
                        {
                            hasEdit = true;
                            break;
                        }
                    }

                    if (!hasEdit) // No changes at all, meaning that our guess is correct!
                    {
                        return new Delete(child);
                    }
                    // Otherwise: continue the next attempt.
                }

                // All attempts fail. These two nodes are in fact quite different.
                return new Update(oldNode, newNode);
            }

            if (k2 - k1 == 1) // One of the children is inserted.
            {
                // We tell the same story.
                foreach (int j in HeuristicOrder(oldChildren, newChildren))
                {
                    var child = newChildren[j];
                    Log.Fine("Guess: insert {0}", child);

                    // Let's see if `child` is inserted.
                    var hasEdit = false;
                    for (int i = 0; i < k1; i++)
                    {
                        var newChild = newChildren[i < j ? i : i + 1];
                        var oldChild = oldChildren[i];
                        var result = Compare(oldChild, newChild);

                        if (result.kind != ResultKind.IDENTICAL)
                        {
                            hasEdit = true;
                            break;
                        }
                    }

                    if (!hasEdit) // No changes at all, meaning that our guess is correct!
                    {
                        return new Insert(oldNode, j, child);
                    }
                    // Otherwise: continue the next attempt.
                }

                // All attempts fail. These two nodes are in fact quite different.
                return new Update(oldNode, newNode);
            }

            // At least 2 children are inserted/deleted: instead of involving multiple edits,
            // why not regard it as a single update?
            Log.Warning("At least 2 children are inserted/deleted: {0} <-> {1}", oldNode, newNode);
            return new Update(oldNode, newNode);
        }

        private static List<int> HeuristicOrder(List<SyntaxNode> less, List<SyntaxNode> more)
        {
            int k = less.Count;
            Debug.Assert(more.Count == k + 1);

            var scores = new double[k + 1];
            for (int j = 0; j < k + 1; j++) // if `more[j]` is the extra/inserted one
            {
                for (int i = 0; i < k; i++)
                {
                    scores[j] += EstimatedSimilarity(more[i < j ? i : i + 1], less[i]);
                }
            }

            return scores
                .Zip(Enumerable.Range(0, k + 1), (s, i) => (Value: s, Index: i))
                .OrderByDescending(p => p.Value) // the higher score, the higher priority
                .Select(p => p.Index).ToList();
        }

        /// <summary>
        /// A heuristic function for estimating the similarity of two trees,
        /// i.e. all descendants shall be considered.
        /// Since this function is simply an estimation, we need to design a fast algorithm,
        /// say we simply compare if their labels and code are the same.
        /// The higher number, the more similar.
        /// </summary>
        /// <param name="node1">The first tree.</param>
        /// <param name="node2">The second tree.</param>
        /// <returns>A numeric value in range [0,1] presenting the similarity.</returns>
        private static double EstimatedSimilarity(SyntaxNode node1, SyntaxNode node2)
        {
            double score = 0;
            if (node1.code == node2.code) score += 0.5;
            if (node1.label.Equals(node2.label)) score += 0.5;

            return score;
        }
    }

    public enum ResultKind
    {
        IDENTICAL, INSERT, DELETE, UPDATE
    }

    /// <summary>
    /// Results showing the tree transformation operations.
    /// </summary>
    public abstract class Result
    {
        public ResultKind kind { get; }

        public SyntaxNode oldTree { get; }

        protected Result(ResultKind kind, SyntaxNode oldTree)
        {
            this.kind = kind;
            this.oldTree = oldTree;
        }

        public SyntaxNode GetTransformed() =>
            Transform(oldTree).Instantiate(new SyntaxNodeContext(), 0);

        protected abstract PartialNode Transform(SyntaxNode node);

        public abstract void PrintTo(IndentPrinter printer);
    }

    /// <summary>
    /// Insertion: insert a `newNode` as the child (with index `k`) of node `oldNodeParent`.
    /// </summary>
    public class Insert : Result
    {
        public SyntaxNode oldNodeParent { get; }
        public int k { get; }
        public SyntaxNode newNode { get; }

        public Insert(SyntaxNode parent, int childIndex, SyntaxNode newNode)
            : base(ResultKind.INSERT, parent.context.root)
        {
            this.oldNodeParent = parent;
            this.k = childIndex;
            this.newNode = newNode;
        }

        public override string ToString() => $"Insert: {oldNodeParent} @child {k} -> {newNode}";

        protected override PartialNode Transform(SyntaxNode node)
        {
            var label = node.label;

            if (node.id == oldNodeParent.id) // Do insertion.
            {
                var builders = node.GetChildren()
                    .Select(x => x.ToPartial()).ToList(); // Clone old children.
                builders.Insert(k, newNode.ToPartial()); // Insert `newNode` at `k`.
                return Node.CreatePartial(label, builders);
            }

            if (node.kind == SyntaxKind.NODE)
            {
                // Insertion shall be done later.
                return Node.CreatePartial(label, node.GetChildren()
                    .Select(Transform).ToList()); // Insertion builder must be used here.
            }

            return node.ToPartial();
        }

        public override void PrintTo(IndentPrinter printer)
        {
            oldNodeParent.PrintTo(printer);
            printer.PrintLine("<->");
            newNode.PrintTo(printer);
        }
    }

    /// <summary>
    /// Deletion: delete the `oldNode`.
    /// </summary>
    public class Delete : Result
    {
        public SyntaxNode oldNode { get; }

        public Delete(SyntaxNode oldNode) : base(ResultKind.DELETE, oldNode.context.root)
        {
            this.oldNode = oldNode;
        }

        override public string ToString() => $"Delete: {oldNode}";

        protected override PartialNode Transform(SyntaxNode node)
        {
            var label = node.label;

            if (node.id == oldNode.parent.id) // Do deletion.
            {
                return Node.CreatePartial(label, node.GetChildren()
                    .Where(x => x.id != oldNode.id) // Filter all children but the `oldNode`.
                    .Select(x => x.ToPartial()).ToList() // Clone them.
                );
            }

            if (node.kind == SyntaxKind.NODE)
            {
                // Deletion shall be done later.
                return Node.CreatePartial(label, node.GetChildren()
                    .Select(Transform).ToList()); // Deletion builder must be used here.
            }

            return node.ToPartial();
        }

        public override void PrintTo(IndentPrinter printer)
        {
            oldNode.PrintTo(printer);
        }
    }

    /// <summary>
    /// Update: replace the `oldNode` with the `newNode`.
    /// </summary>
    public class Update : Result
    {
        public SyntaxNode oldNode { get; }

        public SyntaxNode newNode { get; }

        public Update(SyntaxNode oldNode, SyntaxNode newNode) 
            : base(ResultKind.UPDATE, oldNode.context.root)
        {
            this.oldNode = oldNode;
            this.newNode = newNode;
        }

        override public string ToString() => $"Update: {oldNode} -> {newNode}";

        protected override PartialNode Transform(SyntaxNode node)
        {
            var label = node.label;

            if (node.id == oldNode.parent.id) // Do update.
            {
                return Node.CreatePartial(label, node.GetChildren()
                    .Select(x => x.id == oldNode.id ? newNode.ToPartial() // Use `newNode`.
                        : x.ToPartial()).ToList() // Use old.
                );
            }

            if (node.kind == SyntaxKind.NODE)
            {
                // Update shall be done later.
                return Node.CreatePartial(label, node.GetChildren()
                    .Select(Transform).ToList()); // Update builder must be used here.
            }

            return node.ToPartial();
        }

        public override void PrintTo(IndentPrinter printer)
        {
            oldNode.PrintTo(printer);
            printer.PrintLine("<->");
            newNode.PrintTo(printer);
        }
    }

    /// <summary>
    /// Identical: no transformation.
    /// </summary>
    public class Identical : Result
    {
        public Identical(SyntaxNode oldNode) : base(ResultKind.IDENTICAL, oldNode.context.root) { }

        override public string ToString() => "Identical";

        protected override PartialNode Transform(SyntaxNode node) => node.ToPartial();

        public override void PrintTo(IndentPrinter printer)
        {
        }
    }
}