public class SecondScript : MonoBehaviour
{
    int number1 = 2;
    int number2 = 3;
    int number3 = 7;

    void Start()
    {

        int answer =
        AddTwoNumbers(number1, number2) +
        AddTwoNumbers(number1, number3);

        DisplayResult(answer);

    }


    void AddTwoNumbers(int firstNumber, int secondNumber)
    {

        int result = firstNumber + secondNumber;
        return result;

    }

    void DisplayResult(int total)
    {

        Debug.Log("The grand total is: " + total);

    }


}