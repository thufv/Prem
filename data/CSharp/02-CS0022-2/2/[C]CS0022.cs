// CS0022.cs  
public class MyClass
{
    public static void Main()
    {
        int[,] a = new int[10,10];
        a[0] = 0;     // single-dimension array  
        a[0, 1] = 9;   // CS0022, the array does not have two dimensions  
    }
}