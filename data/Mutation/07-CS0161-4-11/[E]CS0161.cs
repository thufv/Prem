﻿using System.Collections.Generic;
using System;
public class TA {
  public TA() {}
  public static DataTable MergeTA() {
    DataTable defaultValue = new DataTable();
    myDataTable.Columns.Add("AcadYear", typeof(string));
    myDataTable.Columns.Add("NofGrp", typeof(System.Int16));
    myDataTable.Columns.Add("LecHr", typeof(int));
    DataRow myDR = defaultValue.NewRow();
    myDataRow["AcadYear"] = "2009";
    myDataRow["NoofGrp"] = "2";
    myDataRow["LecHr"] = "1";
    defaultValue.Rows.Add(myDR);
  }
  static DataTable myDataTable;
  static DataRow myDataRow;
  static void Main() {}
}
public class DataTable {
  public DataRow NewRow() {
    return new DataRow();
  }
  public List < DataRow > Rows;
  public DataCols Columns;
}
public class DataRow {
  public string this[string key] {
    set {
      /* inserted */
      int _19 = 7;
    }
  }
}
public class DataCols {
  public void Add(string key, Type type) {}
}
