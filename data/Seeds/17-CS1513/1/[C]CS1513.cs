using System;
public class Test
{
    public static void Main()
    {
        string reply;
        Console.WriteLine("Hi");
        reply = Console.ReadLine().ToUpper();
        if (reply == "BYE")
        { Console.WriteLine("bye"); }
    }
}