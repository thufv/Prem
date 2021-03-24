// CS0432.cs  

public class B
{
    public static void Meth() { }
}


public class Test
{
    public static void Main()
    {
        B::Meth();   // CS0432  
                       // To resolve, use the following line instead:  
                       // B.Meth();  
    }
}