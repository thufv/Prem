using System;

class Program
{
    public static void main()
    {
        // Generic class
        FixedSizeCollection<string> gC = new FixedSizeCollection<string>(5);
        Console.WriteLine(gC);

        string s1 = "s1";
        string s2 = "s1";
        string s3 = "s1";
        int i1 = 1;

        gC.AddItem(s1);
        gC.AddItem(s2);
        gC.AddItem(s3);

        gC.AddItem(i1.ToString());
    }
}