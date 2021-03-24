public class Circle
{
    private double radius;
    public Circle(double r)
    {
        radius = r;
    }
    public double diameter()
    {
       double d = radius * 2;
       return d;
    }
}