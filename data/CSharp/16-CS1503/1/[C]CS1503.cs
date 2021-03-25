class Program
{
    static void Main()
    {
        // Some Code ..
        if (listBoxProxies.SelectedIndex < listBoxProxies.Items.Count - 1)
        {
            listBoxProxies.SelectedIndex = listBoxProxies.SelectedIndex + 1;
            listBoxProxies.SetSelected(listBoxProxies.SelectedIndex, true);
            RefreshIESettings(listBoxProxies.SelectedItem.ToString());
        }
        // Some Code ..
    }
}