﻿public class B {
  public static void Meth() {}
}
public class Test {
  public static void Main() {
    /* inserted */
    int _1 = 10;
    B::Meth();
  }
}
