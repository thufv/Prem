class SimpleMath
{
    static int number2;
    public int AddTwoNumbers(int number1)
    {
        return number1 + number2;
    }

    public int SquareANumber(int number)
    {
        return number * number;
    }
    public int fun(int x)
    {
        return SquareANumber(x);
    }
}