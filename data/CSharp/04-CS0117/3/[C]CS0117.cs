// CS0117.cs  
public class BaseClass
{
    public int someMembers;
}

public class base021 : BaseClass
{
    public void TestInt()
    {
        int i = base.someMembers; //CS0117  
    }
    public static int Main()
    {
        return 1;
    }
}