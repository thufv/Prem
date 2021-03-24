using System;
using System.Linq;

public class Test
{
    int[,] a = new int[10,10];
    public static void Main()
    {
        for (int i = 0; i != 10; i++)
        {
            //a[i] = new int[20];
            a[i, 0] = i + 100;
        }
        // some codes
    }
}