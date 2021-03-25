import javax.swing.JOptionPane;
class Overload
{
    void average(int a, int b, int c)
    {
        average= (a+b+c)/3;
    }
    void average(int d, int e, double a, double b)
    {
        average= (d+e+a+b)/4;
    }
    void average(double c,double d,double e,double f,double g)
    {
        average= (c+d+e+f+g)/5;
}

class mainOverload
{
    public static void main(String args[])
    {
        Overload object=new Overload();
        object.average(7, 5, 1);
        object.average(15, 12, 15.12, 12.15);
        object.average(7.7, 8.4, 30.2, 1.4, 6.4);
    }
}
}