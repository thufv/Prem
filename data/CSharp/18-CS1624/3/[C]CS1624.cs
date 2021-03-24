using System;
using System.Collections;

class Program
{
    private bool quest1Started;

    IEnumerator StartedQuest()
    {
        quest1Started = true;

        yield return WaitForSeconds(3);
        quest1Started = false;
    }

    private object WaitForSeconds(int v)
    {
        throw new NotImplementedException();
    }
}