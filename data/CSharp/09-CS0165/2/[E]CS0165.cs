using System;

public struct Vector2
{
    public event EventHandler trigger;

    public float X;
    public float Y;

    public Vector2 func()
    {
        Vector2 vector;
        vector.X = 1;
        vector.Y = 2;
        return vector;  // error CS0165: Use of unassigned local variable 'vector'
    }
}