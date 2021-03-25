using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace testscores
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        private static int[] GetValues()
        {

            string inValue;
            int[] defaultValue = new int[5];
            int total = 0;

            for (int i = 0; i < defaultValue.Length; i++)
            {
                Console.Write("Enter Score {0}: ", i + 1);
                inValue = Console.ReadLine();
                defaultValue[i] = Convert.ToInt32(inValue);
            }

            for (int i = 0; i < defaultValue.Length; i++)
            {
                total += defaultValue[i];
            }
        }
    }
}