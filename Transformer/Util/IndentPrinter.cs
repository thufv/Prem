using System;

namespace Prem.Util
{
    public class IndentPrinter
    {
        int width;

        int level = 0;

        bool newline = true;

        public IndentPrinter(int spaces = 4)
        {
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

        public void Print(string text)
        {
            if (newline)
            {
                Console.Write(new String(' ', width * level));
                newline = false;
            }
            Console.Write(text);
        }

        public void PrintLine(string text)
        {
            Print(text);
            Console.WriteLine();
            newline = true;
        }
    }
}