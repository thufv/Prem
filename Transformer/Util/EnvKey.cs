namespace Prem.Util
{
    public class EnvKey
    {
        public int first { get; }
        
        public int second { get; }

        public EnvKey(int id)
        {
            this.first = id;
            this.second = -1;
        }

        public EnvKey(int first, int second)
        {
            this.first = first;
            this.second = second;
        }

        public override string ToString() => second == -1 ? $"{first}" : $"{first}:{second}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (EnvKey)obj;
            return this.first == that.first && this.second == that.second;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(first.GetHashCode(), second.GetHashCode());
        }
    }
}