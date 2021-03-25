public partial class Form1 : Form
{
    String text = "";

    public Form1()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        String inches = textBox1.Text;
        text = ConvertToFeet(inches) + ConvertToYards(inches);
        textBox2.AppendText(text);
    }

    private String ConvertToFeet(String inches)
    {
        int feet = Convert.ToInt32(inches) / 12;
        int leftoverInches = Convert.ToInt32(inches) % 12;
        return (feet + " feet and " + leftoverInches + " inches." + " \n");
    }

    private String ConvertToYards(String inches)
    {
        int yards = Convert.ToInt32(inches) / 36;
        int feet = (Convert.ToInt32(inches) - yards * 36) / 12;
        int leftoverInches = Convert.ToInt32(inches) % 12;
        return (yards + " yards and " + feet + " feet, and " + leftoverInches + " inches.");
    }
}