// CS0022.cs  
public class MyClass
{
    int[,] a = new int[10,10];
    public static void Main()
    {
        a[0] = 0;     // single-dimension array  
        a[0, 1] = 9;   // CS0022, the array does not have two dimensions  
    }
}