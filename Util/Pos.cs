using System;

namespace Prem.Util
{
    public class Pos
    {
        int line;
        int offset;

        public Pos(int line, int pos)
        {
            this.line = line;
            this.offset = pos;
        }

        override public string ToString()
        {
            return $"({line}, {offset})";
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }

            var that = (Pos)obj;
            return line == that.line && offset == that.offset;
        }
    }
}