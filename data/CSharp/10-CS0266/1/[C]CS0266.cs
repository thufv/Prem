class Program
{
    private MyFinger.MyKAD MyKad = new MyFinger.MyKAD();
    void Button1_Click(object sender, EventArgs e)
    {
        int MyKADSts = 0;
        long value = MyKad.Connect();
        MyKADSts = (int) value;
        //ShowMsg("MyKad.Connect_MyKad():" + MyKADSts);
        //Button3.Enabled = false;
        //Button4.Enabled = false;


    }
}
