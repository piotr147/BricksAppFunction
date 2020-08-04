using System.Data;
using System.Data.SqlClient;

namespace BricksAppFunction
{
    public static class DbUtils
    {
        public static bool UserExists(SqlConnection conn, string mail)
        {
            string query = @"select count(*) from Subscribers where mail = @mail and isdeleted = 0;";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@mail", SqlDbType.VarChar, 50).Value = mail;


                return (int)cmd.ExecuteScalar() > 0;
            }
        }
    }
}
