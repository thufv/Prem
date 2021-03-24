using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{

    void OnCollisionEnter2D(Collision2D col)
    {

        if (col.gameObject.tag == ("Player"))
        {

            Leaderboard.LDRBRD.Score += 1000;

            Destroy(gameObject);
        }
    }
}