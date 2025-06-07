using System;
using System.Data;
using System.Data.SqlClient;

namespace homefix.Helpers
{
    public static class DatabaseHelper
    {
        private const string CONNECTION_STRING = "data source=tcp:mednat.ieeta.pt,8101;initial catalog=;user id=p11g5;password=";//"data source=ZEZOCA;integrated security=true;initial catalog=homefix";

        // Cria uma nova conexão sempre que chamada
        public static SqlConnection GetConnection()
        {
            var cn = new SqlConnection(CONNECTION_STRING);
            cn.Open();
            return cn;
        }

        // Apenas verifica se a conexão pode ser aberta com sucesso
        public static bool VerifyConnection()
        {
            try
            {
                using (var cn = GetConnection())
                {
                    return cn.State == ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
