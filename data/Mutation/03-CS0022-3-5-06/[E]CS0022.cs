﻿using System;
using System.Linq;
public class Test {
  static int[] a = new int[10];
  public static void Main() {
    for (int i = 0; i != 10; i++) {
      a[i, 2
      /* updated */
      ] = i + 100;
    }
  }
}
