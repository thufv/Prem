using System;

class Program
{
    static void Main()
    {
        try
        {

        }
        catch(Exception ex) {
            string MyException = ex.ToString;
            Console.WriteLine(MyException);
        }


    }
}
