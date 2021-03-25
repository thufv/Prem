using System.Collections.Generic;

class Program
{
    public static void Main()
    {
        List<TaggedEdge<int, float>> enumerable = new List<TaggedEdge<int, float>>();

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