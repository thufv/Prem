using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MethodeReturn : MonoBehaviour
{

    // Ma methode
    static void LongHypo(float a, float b)
    {
        float SommeCar = a * a + b * b;
        return SommeCar;
    }

    // Use this for initialization
    void Start()
    {
        float result = LongHypo(3, 4);
        Debug.Log(result);

    }

    // Update is called once per frame
    void Update()
    {

    }
}