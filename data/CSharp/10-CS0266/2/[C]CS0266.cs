class Program
{
    public static object sqlDefaultTime;
    public static void main()
    {
        int dd = 6000;
        sqlCmdDefaultTime = new SqlCommand("myQuery", sqlCon);
        sqlDefaultTime = sqlCmdDefaultTime.ExecuteReader();
        while (sqlDefaultTime.Read())
        {
            dd = (int) sqlDefaultTime;
        }
    }
}
