using System;

public class Object2
{
    private object value;

    public Object2(object value)
    {
        this.value = value;
    }

    public override string ToString()
    {
        return base.ToString() + ": " + value.ToString();
    }
}

public class Example
{
    public static void Main()
    {
        Object2 obj2 = new Object2('a');
        func(obj2.ToString());
    }
    public static void func(string s)
    {

    }
}
// The example displays the following output:
//       Object2: a