// CS0122.cs  
public class MyClass
{
    // Make public to resolve CS0122  
    public void MyMethod()
    {
    }
}

public class MyClass2
{
    public static int Main()
    {
        MyClass a = new MyClass();
        // MyMethod is private  
        a.MyMethod();   // CS0122  
        return 0;
    }
}