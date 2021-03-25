// using UnityEngine;
using System.Collections;

public class ChangeScene //: MonoBehaviour
{

    public IEnumerator ChangeToScene(string sceneToChangeTo)
    {
        float fadeTime = 1.0F;
        yield return new WaitForSeconds(fadeTime);
        Application.LoadLevel(sceneToChangeTo);
    }

    public static void Main() {}
}

class WaitForSeconds
{
    public WaitForSeconds(float time) {}
}

class Application
{
    public static void LoadLevel(string level) {}
}