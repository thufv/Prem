﻿using System;
namespace DecisionMaking {
  class Program {
    static void Main(string[] args) {
      int a = 100;
      if (a == 10) {
        /* inserted */
        int _17 = 16;
        Console.WriteLine("Value of a is 10");
      } else if (a == 20) {
        Console.WriteLine("Value of a is 20");
      } else if (a == 30) {
        Console.WriteLine("Value of a is 30");
      } else {
        Console.WriteLine("None of the values is matching");
      }
      Console.WriteLine("Exact value of a is: {0}", a);
      Console.ReadLine();
    }
  }
