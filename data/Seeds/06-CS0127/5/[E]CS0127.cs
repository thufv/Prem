using System;

public class SecondScript //: MonoBehaviour
{
    int number1 = 2;
    int number2 = 3;
    int number3 = 7;

    void AddTwoNumbers(int firstNumber, int secondNumber)
    {

        int result = firstNumber + secondNumber;
        return result;

    }

    void DisplayResult(int total)
    {

        Console.WriteLine("The grand total is: " + total);

    }

    static void Main() {}
}