using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace NDR.HL72FHIR.Helper
{
    class ConnectionStringHelper
    {
        public static string GetHL7LoadConnectionStringValue()
        {
            string user = "pyro_admin";
            string password = "Fhirpoc123";
            string connString = "Server=pyropoc.database.windows.net;Database=HL7Load;User ID=" + user + ";Password=" + password + ";";

            return connString;// ConfigurationManager.ConnectionStrings["WarehouseConnectionString"].ConnectionString;
        }

        public static DataTable GetDataFromSQLtable(string sql)
        {
            SqlConnection sqlConnection = new SqlConnection(GetHL7LoadConnectionStringValue());
            DataSet ds = new DataSet();

            try
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter(sql, sqlConnection);
                sqlConnection.Open();
                sqlAdapter.Fill(ds, "Data");
            }
            catch (Exception ex)
            {
                // Catch the error
            }
            finally
            {
                sqlConnection.Close();
            }

            return ds.Tables["Data"];
        }
    }
}
