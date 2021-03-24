// CS0161.cs  
public class Test
{
    public static int Main() // CS0161  
    {
        int defaultValue = 10;
        if (defaultValue < 10)
        {
            return defaultValue;
        }
        // else
        // {
            // uncomment following lines to resolve
            defaultValue = 1;
            return defaultValue;  
        // }
    }
}