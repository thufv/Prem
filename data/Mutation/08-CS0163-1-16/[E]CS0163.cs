﻿class Program {
  static void Main(string[] args) {
    string destination = "default";
    /* inserted */
    int _2 = 25;
    switch (destination) {
    case "Inventory":
      AddToInventory();
    case "Hotbar":
      AddToHotbar();
      break;
    default:
      Debug.Log("Destination Unknown");
      break;
    }
  }
  static void AddToInventory() {}
  static void AddToHotbar() {}
}
class Debug {
  public static void Log(string v) {}
}
