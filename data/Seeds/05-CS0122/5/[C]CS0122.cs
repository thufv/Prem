using System;

class Square
{
    public Square(double side)
    {
        Side = side;
    }

    public double Side { get; }
}

class AreaCalculator
{
    public double Calculate(Square square) => square.Side * square.Side;
}

public class Program
{
    public static void Main(string[] args)
    {
        var square = new Square(4); // internal type
        var calculator = new AreaCalculator(); // internal type
        var area = calculator.Calculate(square); // private method
        Console.WriteLine($"Square with a side of 4 has an area of {area}");
    }
}