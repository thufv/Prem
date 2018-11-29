namespace Prem.Util {
    public class DiffResult
    {
        public enum Kind
        {
            INSERT, DELETE, UPDATE, FAILURE
        }

        public Kind kind { get; }

        public SyntaxNode source { get; }

        public SyntaxNode target { get; }

        public int k { get; }

        private DiffResult()
        {
            this.kind = Kind.FAILURE;
        }

        private DiffResult(SyntaxNode source, int k, SyntaxNode target)
        {
            this.kind = Kind.INSERT;
            this.source = source;
            this.k = k;
            this.target = target;
        }

        private DiffResult(SyntaxNode source)
        {
            this.kind = Kind.DELETE;
            this.source = source;
        }

        private DiffResult(SyntaxNode source, SyntaxNode target)
        {
            this.kind = Kind.UPDATE;
            this.source = source;
            this.target = target;
        }

        public static DiffResult FAILURE()
        {
            return new DiffResult();
        }

        public static DiffResult Insert(SyntaxNode parent, int childIndex, SyntaxNode target)
        {
            return new DiffResult(parent, childIndex, target);
        }

        public static DiffResult Delete(SyntaxNode where)
        {
            return new DiffResult(where);
        }

        public static DiffResult Update(SyntaxNode where, SyntaxNode target)
        {
            return new DiffResult(where, target);
        }
    }
}