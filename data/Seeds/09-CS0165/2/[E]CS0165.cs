using System.Collections.Generic;

class Program
{
    public static void Main()
    {
        List<TaggedEdge<int, float>> enumerable;

        if (IsSingle(enumerable))
        {
        }
    }

    static bool IsSingle<T>(List<T> list)
    {
        return true;
    }
}

public class TaggedEdge<A, B>
{

}