public class DebugThree1
{
    public static int calcTip(double bill)
    {
        final double RATE = 0.15;
        final double tip = bill * RATE;
        int ret = (int) tip;
        return ret;
    }
}