using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace homefix.Helpers
{
    public static class DatabaseHelper
    {
        private static SqlConnection cn;

        public static SqlConnection GetConnection()
        {
            if (cn == null)
                cn = new SqlConnection("data source=ZEZOCA;integrated security=true;initial catalog=homefix");
            return cn;
        }

        public static bool VerifyConnection()
        {
            if (cn == null)
                cn = GetConnection();

            if (cn.State != ConnectionState.Open)
                cn.Open();

            return cn.State == ConnectionState.Open;
        }
    }
}
