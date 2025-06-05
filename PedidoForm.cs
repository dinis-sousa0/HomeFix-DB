using homefix.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace homefix
{
    public partial class PedidoForm : Form
    {
        private string emailUtilizador;
        private int clienteID;
        public PedidoForm(string email)
        {
            InitializeComponent();
            emailUtilizador = email;
            clienteID = ObterClienteIDPorEmail(emailUtilizador);
            textBox3.Text = clienteID.ToString();
            comboBox1.Items.AddRange(new string[] { "MBWay", "Dinheiro", "Cartão" });

            // Opcional: carregar dados iniciais em outras abas se desejar
        }

        private int ObterClienteIDPorEmail(string email)
        {
            if (!DatabaseHelper.VerifyConnection())
                throw new Exception("Erro na conexão com a base de dados.");

            string sql = "SELECT ID_Utilizador FROM Utilizador WHERE Email = @Email";
            using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                object result = cmd.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int id))
                    return id;
                else
                    throw new Exception("Cliente não encontrado.");
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string localizacao = textBox1.Text.Trim();
            string descricao = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(localizacao) || string.IsNullOrEmpty(descricao))
            {
                MessageBox.Show("Por favor, preencha todos os campos.");
                return;
            }

            try
            {
                string estado = "Pendente";
                DateTime dataPedido = DateTime.Now;

                string sql = @"INSERT INTO PedidoServico (Localizacao, Estado, data_pedido, Descricao, Cliente)
                               VALUES (@localizacao, @estado, @dataPedido, @descricao, @clienteID)";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@localizacao", localizacao);
                    cmd.Parameters.AddWithValue("@estado", estado);
                    cmd.Parameters.AddWithValue("@dataPedido", dataPedido);
                    cmd.Parameters.AddWithValue("@descricao", descricao);
                    cmd.Parameters.AddWithValue("@clienteID", clienteID);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        MessageBox.Show("Pedido criado com sucesso!");
                        tabControl1.SelectedTab = tabPage2; // Vai para pendentes
                        CarregarPedidosPorEstado("Pendente");
                    }
                    else
                    {
                        MessageBox.Show("Erro ao criar o pedido.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage2)
            {
                CarregarPedidosPorEstado("Pendente");
            }
            else if (tabControl1.SelectedTab == tabPage3)
            {
                CarregarPedidosPorEstado("Progresso");
            }
            else if (tabControl1.SelectedTab == tabPage4)  // Exemplo de aba 4 para pedidos concluídos
            {
                CarregarPedidosPorEstado("Concluido");
            }
        }

        private void CarregarPedidosPorEstado(string estado)
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = @"SELECT ID_pedido, Localizacao, data_pedido, Descricao, Estado, Servico
                               FROM PedidoServico
                               WHERE Cliente = @clienteID AND Estado = @estado";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@clienteID", clienteID);
                    cmd.Parameters.AddWithValue("@estado", estado);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        if (estado == "Pendente") { dataGridView1.DataSource = dt; }
                        else if (estado == "Progresso") { dataGridView2.DataSource = dt; }
                        else { dataGridView3.DataSource = dt; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos: " + ex.Message);
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close(); // Fecha o form atual (importante para libertar memória)
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView3.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um pedido primeiro.");
                return;
            }

            int idPedido = Convert.ToInt32(dataGridView3.SelectedRows[0].Cells["ID_pedido"].Value);

            string comentario = textBox5.Text.Trim();
            if (!decimal.TryParse(textBox4.Text, out decimal rating) || rating < 0 || rating > 5)
            {
                MessageBox.Show("Insira um rating válido (0-5).");
                return;
            }

            // Obtem serviço e profissional do pedido
            string sql = @"SELECT S.Num_servico, S.Profissional
                   FROM PedidoServico P
                   JOIN Servico S ON P.Servico = S.Num_servico
                   WHERE P.ID_pedido = @idPedido";

            using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@idPedido", idPedido);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int servicoID = reader.GetInt32(0);
                        int profissionalID = reader.GetInt32(1);

                        reader.Close();

                        // Inserir avaliação
                        string insertSql = @"INSERT INTO Avaliacao (Servico, Profissional, Comentario, Rating, data_aval)
                                     VALUES (@servico, @profissional, @comentario, @rating, @data)";
                        using (SqlCommand insertCmd = new SqlCommand(insertSql, DatabaseHelper.GetConnection()))
                        {
                            insertCmd.Parameters.AddWithValue("@servico", servicoID);
                            insertCmd.Parameters.AddWithValue("@profissional", profissionalID);
                            insertCmd.Parameters.AddWithValue("@comentario", comentario);
                            insertCmd.Parameters.AddWithValue("@rating", rating);
                            insertCmd.Parameters.AddWithValue("@data", DateTime.Now);

                            int rows = insertCmd.ExecuteNonQuery();
                            if (rows > 0)
                            {
                                MessageBox.Show("Avaliação enviada com sucesso!");
                                panel1.Visible = false;
                                textBox5.Clear();
                                textBox4.Clear();
                            }
                            else
                            {
                                MessageBox.Show("Erro ao enviar avaliação.");
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Erro ao obter dados do serviço.");
                    }
                }
            }
        }


        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView3.SelectedRows.Count > 0)
            {
                panel1.Visible = true;

                int servicoID = Convert.ToInt32(dataGridView3.SelectedRows[0].Cells["Servico"].Value);

                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro de ligação à base de dados.");
                    return;
                }

                string sql = @"SELECT COUNT(*) FROM Pagamento 
                       WHERE Servico = @ServicoID AND Cliente = @ClienteID";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);

                    int count = (int)cmd.ExecuteScalar();

                    if (count == 0)
                    {
                        panel2.Visible = true;
                    }
                    else
                    {
                        panel2.Visible = false;
                        MessageBox.Show("Pagamento já foi efetuado para este serviço.");
                    }
                }
            }
            else
            {
                panel1.Visible = false;
                panel2.Visible = false;
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView3.SelectedRows.Count == 0) return;

            int servicoID = Convert.ToInt32(dataGridView3.SelectedRows[0].Cells["Servico"].Value);
            string tipo = comboBox1.SelectedItem?.ToString();
            string sumario = textBox6.Text.Trim();

            if (string.IsNullOrEmpty(tipo) || string.IsNullOrEmpty(sumario))
            {
                MessageBox.Show("Preencha todos os campos do pagamento.");
                return;
            }

            try
            {
                string sql = @"INSERT INTO Pagamento (Servico, Cliente, Sumario, Tipo)
                       VALUES (@ServicoID, @ClienteID, @Sumario, @Tipo)";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);
                    cmd.Parameters.AddWithValue("@Sumario", sumario);
                    cmd.Parameters.AddWithValue("@Tipo", tipo);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        MessageBox.Show("Pagamento registado com sucesso!");
                        panel2.Visible = false;
                    }
                    else
                    {
                        MessageBox.Show("Erro ao registar pagamento.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

    }
}

