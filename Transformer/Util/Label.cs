namespace Prem.Util
{
    public class Label
    {
        public int id { get; }

        public string name { get; }

        public Label(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString() => $"{id}:{name}";

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Label)obj;
            return this.id == that.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }
}