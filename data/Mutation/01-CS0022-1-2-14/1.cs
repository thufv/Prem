public class MyClass {
  public static void Main() {
    int[] a = new int[10];
    a[0] = 0;
    /* inserted */
    a[0] = 5;
    a[0, 1] = 9;
  }
}