using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace homefix.Helpers
{
    public static class DataLoader
    {
        public static void CarregarEspecializacoes(ComboBox comboBox)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = new SqlCommand("SELECT Nome FROM Especializacao", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    comboBox.Items.Clear();

                    while (reader.Read())
                    {
                        comboBox.Items.Add(reader["Nome"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar especializações: " + ex.Message);
            }
        }
    }
}
