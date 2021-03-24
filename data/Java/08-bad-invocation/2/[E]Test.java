import sun.security.util.Length;

class IntList {
    public int[] data;
    public int length() {
        return data.length;
    }
}

public class Test{
    public static void my_method() {
        IntList il = new IntList();
        System.out.println(il.length);
    }
}