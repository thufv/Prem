﻿public class D {
  public virtual void DoWork(int i) {}
}
public abstract class E: D {
  public abstract override void DoWork(int i);
}
public class F: E {
  public override void DoWork(int i) {
    /* inserted */
    int _19 = 13;
  }
  public static void Main() {}
