class Program
{
    public IEnumerator ChangeCharacter()
    {
        // 「yield return new WaitForSeconds(0.5f);」の存在が原因でエラー
        GameObject FlickManagerObj = GameObject.Find("FlickManager").gameObject;
        CurrentType = Type;
        Case[CurrentType].transform.FindChild("TimerGameOverScript").GetComponent<TimerGameOver>().StopRunTime();
        yield return new WaitForSeconds(0.5f);
        SelectCase(CurrentType);

    }
}
