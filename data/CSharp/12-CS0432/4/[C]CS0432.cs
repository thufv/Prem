
public class Automobile
{
    public static int NumberOfWheels = 4;
    public static int SizeOfGasTank
    {
        get
        {
            return 15;
        }
    }
    public static void Drive() { }
 

    // Other non-static fields and properties...
}

public class cla
{
    void func()
    {
        Automobile.Drive();
    }
}