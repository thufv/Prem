public class TA
{
    public TA()
    {
    }

    public static DataTable MergeTA()
    {

        DataTable defaultValue = new DataTable();
        myDataTable.Columns.Add("AcadYear", typeof(string));
        myDataTable.Columns.Add("NofGrp", typeof(System.Int16));
        myDataTable.Columns.Add("LecHr", typeof(int));
        /*
        ...
        ...
        ...
        */
        DataRow myDR = defaultValue.NewRow();
        myDataRow["AcadYear"] = "2009";
        myDataRow["NoofGrp"] = "2";
        myDataRow["LecHr"] = "1";
        /*
        ...
        ...
        ...
        */
        defaultValue.Rows.Add(myDR);
 

    }

}