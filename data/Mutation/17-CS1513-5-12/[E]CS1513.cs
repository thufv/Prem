﻿public class D {
  public virtual void DoWork(int i) {}
}
public abstract class E: D {
  public abstract override void DoWork(int i);
}
public class F: E {
  public override void DoWork(int i) {}
  public static void Main() {
    /* inserted */
    int _2 = 29;
  }
