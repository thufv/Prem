class Program
{
    public void func(string searchType)
    {
        switch (searchType)
        {
            case "SearchBooks":
                Selenium.Type("//*[@id='SearchBooks_TextInput']", searchText);
                Selenium.Click("//*[@id='SearchBooks_SearchBtn']");
                break;
            case "SearchAuthors":
                Selenium.Type("//*[@id='SearchAuthors_TextInput']", searchText);
                Selenium.Click("//*[@id='SearchAuthors_SearchBtn']");
                break;
        }
    }
}