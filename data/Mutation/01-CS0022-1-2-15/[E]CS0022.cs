﻿public class MyClass {
  public static void Main() {
    int[] a = new int[10];
    a[0] = 0;
    /* inserted */
    int _16 = a[0] = 0;
    a[0, 1] = 9;
  }
}
