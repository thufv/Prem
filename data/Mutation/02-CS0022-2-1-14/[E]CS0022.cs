﻿public class MysScript: MonoBehaviour {
  void Start() {
    int[] myArray = new int[10];
    /* inserted */
    myArray = 20;
    Debug.Log(myArray[0, 0]);
  }
}
public class MonoBehaviour {}
internal class Debug {
  internal static void Log(int v) {}
}
class Program {
  static void Main(string[] args) {}
}
