public class Test {
    public int NewCard()
    {
        int ans = JOptionPane.showConfirmDialog(null, "Do you wish another card?", "7 in 1", JOptionPane.YES_NO_CANCEL_OPTION );
        if (ans == JOptionPane.YES_OPTION)
        {
            ans = 1;
        }

        if (ans == JOptionPane.NO_OPTION)
        {
            ans = 2;
        }

        if (resp == JOptionPane.CANCEL_OPTION)
        {
            ans = 3;
        }
        return ans;
    }
}