using System;

namespace Treehouse.CodeChallenges
{
    class Program
    {
        static void Main()
        {
            while (true)  //loop started
            {
                Console.Write("Enter the number of times to print \"Yay!\": ");
                string entry = Console.ReadLine();

                if (entry.ToLower() == "quit")
                {
                    break;
                }
                else
                {
                    int time = int.Parse(entry);  //turn "string" into "int"
                    int k;
                    for (k = 0; k < time;)  //make the loop run k times
                    {
                        Console.Write("Yay!");
                        k = k + 1;
                        continue;
                    }
                }

                break;  //leave the "while loop" since "for" is not satisfied
            }
        }
    }
}