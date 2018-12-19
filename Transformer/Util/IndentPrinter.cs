using System;

namespace Prem.Util
{
    public class IndentPrinter
    {
        int width;

        int level = 0;

        int maxLevel;

        bool newline = true;

        public IndentPrinter(int maxLevel = 4, int spaces = 4)
        {
            this.maxLevel = maxLevel;
            this.width = spaces;
        }

        public void IncIndent()
        {
            this.level++;
        }

        public void DecIndent()
        {
            this.level--;
        }

        bool normal = true;

        private bool CheckState()
        {
            if (normal)
            {
                if (level > maxLevel)
                {
                    normal = false;
                    Console.Write(new String(' ', width * level));
                    Console.WriteLine("...");
                }
            }
            else
            {
                if (level <= maxLevel)
                {
                    normal = true;
                }
            }

            return normal;
        }

        public void Print(string text, bool endline = false)
        {
            if (CheckState())
            {
                if (newline)
                {
                    Console.Write(new String(' ', width * level));
                    newline = false;
                }
                Console.Write(text);

                if (endline)
                {
                    Console.WriteLine();
                    newline = true;
                }
            }
        }

        public void PrintLine(string text) => Print(text, true);
    }
}