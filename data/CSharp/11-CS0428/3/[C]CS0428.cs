using System;

class Program
{
    static void Main()
    {
        //using (connection = new SqlConnection(ConfigurationManager.AppSettings[connString]))
        {
            //using (command = new SqlCommand(insert into table1(forename, surname) OUTPUT INSERT IGNORE ED.ID values(@forename, @surname), connection))
            {
                command.Parameters.Add(@forename, SqlDbType.VarChar, 255).Value = forename;
                command.Parameters.Add(@surname, SqlDbType.VarChar, 255).Value = surname;

                command.Connection.Open();
                id = (int)command.ExecuteScalar();
            }
        }
    }
}
