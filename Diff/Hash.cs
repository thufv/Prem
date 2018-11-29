namespace Prem.Diff
{
    public static class Hash
    {
        public static int Combine(int hash1, int hash2)
        {
            int hash = 17;
            hash = hash * 31 + hash1;
            hash = hash * 31 + hash2;
            return hash;
        }
    }
}