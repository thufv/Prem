using UnityEngine;
using System.Collections;
using System;

public class test : MonoBehaviour
{
    public GameObject thePlatform;
    public Transform generationPoint;
    public float distanceBetween;
    private float platformWidth;
    // Use this for initialization
    void Start()
    {
        platformWidth = thePlatform.GetComponnent<BoxCollider2D>().size.x;
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public class GameObject
{
    internal ObjectA GetComponent<T>()
    {
        throw new NotImplementedException();
    }
}