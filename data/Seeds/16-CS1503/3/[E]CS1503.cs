class Program
{
    static void Main()
    {
        // Some Code ..
        if (listBoxProxies.SelectedIndex < - 1)
        {
            listBoxProxies.SelectedIndex = listBoxProxies.SelectedIndex + 1;
            listBoxProxies.SetSelected(listBoxProxies.SelectedIndex, true);
            RefreshIESettings(listBoxProxies.SelectedItem);
        }
        // Some Code ..
    }

    static Proxy listBoxProxies;

    static void RefreshIESettings(string config)
    {
        
    }
}

public class Proxy
{
    public int SelectedIndex;

    public void SetSelected(int index, bool b)
    {

    }

    public int SelectedItem = 2333;
}
