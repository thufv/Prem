using UnityEngine;
using System.Collections;

public class ChangeScene : MonoBehaviour
{

    public IEnumerator ChangeToScene(string sceneToChangeTo)
    {
        float fadeTime = GameObject.Find("_Manager").GetComponent<Fading>().BeginFade(1);
        yield return new WaitForSeconds(fadeTime);
        Application.LoadLevel(sceneToChangeTo);
    }
}