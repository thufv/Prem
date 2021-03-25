using System.Collections;
using System.Collections.Generic;
// using UnityEngine;

public class moving : MonoBehaviour {
  public GameObject obj;
  private float speed = 5f,rotSpeed = 2f;
  private Rigidbody2D rb;
  private SpriteRenderer spr;
  float rotation;

  private void Awake(){
    rb = GetRigidbody2D();
    spr = GetSpriteRenderer();
  }

  private void Run(){
    rotation = rotSpeed * Input.GetAxis("Horizontal");
    if (Input.GetAxis ("Horizontal") == 1f || Input.GetAxis("Horizontal") == -1f) {
      rotation =+ rotSpeed;
    }
    transform.rotation = Quaternion (new Vector3 (transform.rotation, transform.rotation, rotation));
    Vector3 direction = transform.up * Input.GetAxis ("Vertical");
    transform.position = Vector3.MoveTowards (transform.position, transform.position + direction,speed * Time.deltaTime);
  }

  private void Update () {
    Run ();
  }

  private Transform transform = new Transform();
}


public class MonoBehaviour
{
  public Rigidbody2D GetRigidbody2D()
  {
    return new Rigidbody2D();
  }

  public SpriteRenderer GetSpriteRenderer()
  {
    return new SpriteRenderer();
  }
}

public class GameObject
{

}

public class Rigidbody2D
{

}

public class SpriteRenderer
{

}

public class Vector3
{
    public Vector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3(Quaternion q1, Quaternion q2, float z)
    {
        this.x = z;
        this.y = z;
        this.z = z;
    }

    public float x, y, z;

    public static Vector3 operator+ (Vector3 l, Vector3 r)
    {
        return new Vector3(l.x + r.x, l.y + r.y, l.z + r.z);
    }

    public static Vector3 operator* (Vector3 l, Vector3 r)
    {
        return new Vector3(l.x * r.x, l.y * r.y, l.z * r.z);
    }

    public static Vector3 operator* (Vector3 l, float r)
    {
        return new Vector3(l.x * r, l.y * r, l.z * r);
    }

    public static Vector3 MoveTowards(Vector3 l, Vector3 r, float t)
    {
      return l * r;
    }
}

public class Quaternion
{
    public Quaternion(Vector3 z)
    {
    }
}

public class Input
{
    public static bool GetMouseButton(int x)
    {
        return x > 0;
    }

    public static float GetAxis(string name)
    {
        return name.Length;
    }
}

public class Transform
{
    public Vector3 position, up;
    
    public Quaternion rotation;
}

public class Time
{
  public static float deltaTime;
}

public class Program
{
  public static void Main()
  {
    
  }
}
