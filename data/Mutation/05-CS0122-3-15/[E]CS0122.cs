﻿public class MyClass {
  private void MyMethod() {
    /* inserted */
    int _15 = 17;
  }
}
public class MyClass2 {
  public static int Main() {
    MyClass a = new MyClass();
    a.MyMethod();
    return 0;
  }
}
