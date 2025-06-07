using System;
using System.Collections.Generic;
using System.Data;
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
                using (SqlCommand cmd = new SqlCommand("spCarregarEspecializacoes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        comboBox.Items.Clear();
                        while (reader.Read())
                        {
                            comboBox.Items.Add(reader["Nome"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar especializações: " + ex.Message);
            }
        }
        public static int ObterUtilizadorIDPorEmail(string email)
        {
            if (!DatabaseHelper.VerifyConnection())
                throw new Exception("Erro na conexão com a base de dados.");

            using (SqlCommand cmd = new SqlCommand("spObterUtilizadorIDPorEmail", DatabaseHelper.GetConnection()))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Email", email);

                object result = cmd.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int id))
                    return id;
                else
                    throw new Exception("Utilizador não encontrado.");
            }
        }
        public static decimal? ObterRatingProfissional(int profissionalID)
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return null;
                }

                using (SqlCommand cmd = new SqlCommand("spObterRatingProfissional", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Parâmetro de entrada
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);

                    // Parâmetro de saída
                    SqlParameter outputParam = new SqlParameter("@Rating", SqlDbType.Decimal)
                    {
                        Precision = 5,
                        Scale = 2,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    // Executa a SP
                    cmd.ExecuteNonQuery();

                    // Verifica o valor do parâmetro de saída
                    if (outputParam.Value != DBNull.Value)
                        return Convert.ToDecimal(outputParam.Value);
                    else
                        return null; // Sem avaliações
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar rating: " + ex.Message);
                return null;
            }
        }
        public static string RegistarPagamento(int servicoID, int clienteID, string sumario, string tipoNome)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = new SqlCommand("spRegistarPagamento", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);
                    cmd.Parameters.AddWithValue("@Sumario", sumario);
                    cmd.Parameters.AddWithValue("@TipoNome", tipoNome);

                    SqlParameter outputParam = new SqlParameter("@Resultado", SqlDbType.NVarChar, 100)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    return outputParam.Value.ToString();
                }
            }
            catch (Exception ex)
            {
                return "Erro: " + ex.Message;
            }
        }
        public static bool ObterDadosProfissionalPorServico(int servicoID,
    out string nome, out string email, out string telefone, out string especializacao,
    out string rating, out string empresa)
        {
            nome = email = telefone = especializacao = rating = empresa = string.Empty;

            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = new SqlCommand("spObterDadosProfissionalPorServico", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            nome = "Nome: " + reader["Nproprio"].ToString();
                            email = "Email: " + reader["Email"].ToString();
                            telefone = "Telefone: " + reader["Telefone"].ToString();
                            especializacao = "Especializacao: " + reader["Especializacao"].ToString();

                            rating = reader["Media_rating"] != DBNull.Value
                                ? "Rating médio: " + Convert.ToDecimal(reader["Media_rating"]).ToString("0.00")
                                : "Rating médio: Sem avaliações";

                            empresa = reader["Nome_empresa"] != DBNull.Value
                                ? "Empresa: " + reader["Nome_empresa"].ToString()
                                : "Empresa: Sem empresa";

                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao obter dados do profissional: " + ex.Message);
            }

            return false;
        }
    }

}
