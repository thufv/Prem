﻿using System;
class Program {
  static void Main() {
    try {} catch(Exception ex) {
      string MyException = ex.ToString;
      /* inserted */
      int _10 = 10;
      Console.WriteLine(MyException);
    }
  }
}
