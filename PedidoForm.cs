using homefix.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
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
            CarregarTiposPagamento();
            DataLoader.CarregarEspecializacoes(comboBox2);

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

        private void CarregarTiposPagamento()
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = "SELECT Tipo FROM Tipo_Pagamento ORDER BY Tipo";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        comboBox1.Items.Clear(); // Limpa itens atuais

                        while (reader.Read())
                        {
                            comboBox1.Items.Add(reader["Tipo"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar tipos de pagamento: " + ex.Message);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string localizacao = textBox1.Text.Trim();
            string descricao = textBox2.Text.Trim();
            string nomeEspecializacao = comboBox2.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(localizacao) || string.IsNullOrEmpty(descricao) || string.IsNullOrEmpty(nomeEspecializacao))
            {
                MessageBox.Show("Por favor, preencha todos os campos e selecione uma especialização.");
                return;
            }

            try
            {
                string estado = "Pendente";
                DateTime dataPedido = DateTime.Now;

                using (SqlCommand cmd = new SqlCommand("sp_InserirPedidoServico", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Localizacao", localizacao);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@DataPedido", dataPedido);
                    cmd.Parameters.AddWithValue("@Descricao", descricao);
                    cmd.Parameters.AddWithValue("@Cliente", clienteID);
                    cmd.Parameters.AddWithValue("@NomeEspecializacao", nomeEspecializacao);

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
                CarregarPedidosConcluidos(); // Novo método específico
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

                using (SqlCommand cmd = new SqlCommand("sp_ObterPedidosPorEstado", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@clienteID", clienteID);
                    cmd.Parameters.AddWithValue("@estado", estado);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        if (estado == "Pendente")
                        {
                            dataGridView1.DataSource = dt;
                        }
                        else if (estado == "Progresso")
                        {
                            dataGridView2.DataSource = dt;
                        }
                        else
                        {
                            dataGridView3.DataSource = dt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos: " + ex.Message);
            }
        }

        private void CarregarPedidosConcluidos()
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                // 1. Pedidos concluídos sem pagamento (para "Por Pagar")
                string sqlPorPagar = @"
            SELECT P.ID_pedido, P.Localizacao, P.data_pedido, P.Descricao, P.Estado, P.Servico
            FROM PedidoServico P
            LEFT JOIN Pagamento Pg ON P.Servico = Pg.Servico AND Pg.Cliente = @clienteID
            WHERE P.Cliente = @clienteID AND P.Estado = 'Concluido' AND Pg.ID_transacao IS NULL";

                using (SqlCommand cmd1 = new SqlCommand(sqlPorPagar, DatabaseHelper.GetConnection()))
                {
                    cmd1.Parameters.AddWithValue("@clienteID", clienteID);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd1))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView3.DataSource = dt;
                    }
                }

                // 2. Todos pedidos concluídos (para "Todos")
                string sqlTodos = @"
            SELECT 
                P.ID_pedido, 
                P.Localizacao, 
                P.data_pedido, 
                P.Descricao, 
                P.Estado, 
                P.Servico
            FROM PedidoServico P
            JOIN Servico S ON P.Servico = S.Num_servico
            JOIN Pagamento PAG ON PAG.Servico = S.Num_servico AND PAG.Cliente = P.Cliente
            WHERE 
                P.Cliente = @clienteID 
                AND P.Estado = 'Concluido'
                AND PAG.ID_transacao IS NOT NULL";

                using (SqlCommand cmd2 = new SqlCommand(sqlTodos, DatabaseHelper.GetConnection()))
                {
                    cmd2.Parameters.AddWithValue("@clienteID", clienteID);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd2))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView4.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos concluídos: " + ex.Message);
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
            string tipoNome = comboBox1.SelectedItem?.ToString();
            string sumario = textBox6.Text.Trim();

            if (string.IsNullOrEmpty(tipoNome) || string.IsNullOrEmpty(sumario))
            {
                MessageBox.Show("Preencha todos os campos do pagamento.");
                return;
            }

            try
            {
                // 1. Obter o ID do tipo de pagamento
                int tipoID = -1;
                string sqlTipo = "SELECT ID_Tipo FROM Tipo_Pagamento WHERE Tipo = @TipoNome";

                using (SqlCommand cmdTipo = new SqlCommand(sqlTipo, DatabaseHelper.GetConnection()))
                {
                    cmdTipo.Parameters.AddWithValue("@TipoNome", tipoNome);
                    object result = cmdTipo.ExecuteScalar();

                    if (result == null)
                    {
                        MessageBox.Show("Tipo de pagamento inválido.");
                        return;
                    }

                    tipoID = Convert.ToInt32(result);
                }

                // 2. Inserir o pagamento com o tipoID correto
                string sql = @"INSERT INTO Pagamento (Servico, Cliente, Sumario, Tipo)
                       VALUES (@ServicoID, @ClienteID, @Sumario, @Tipo)";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);
                    cmd.Parameters.AddWithValue("@Sumario", sumario);
                    cmd.Parameters.AddWithValue("@Tipo", tipoID);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        MessageBox.Show("Pagamento registado com sucesso!");
                        panel2.Visible = false;

                        // Atualiza a lista de pedidos concluídos para refletir o pagamento
                        CarregarPedidosConcluidos();
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

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                // Ocultar painel se não houver seleção
                panel3.Visible = false;
                return;
            }

            try
            {
                int servicoID = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["Servico"].Value);

                string sql = @"
        SELECT U.Nproprio, U.Email, U.Telefone, P.Especializacao, P.Media_rating, E.Nome_empresa
        FROM Servico S
        JOIN Profissional P ON S.Profissional = P.ID
        JOIN Utilizador U ON P.ID = U.ID_Utilizador
        LEFT JOIN Empresa E ON P.Empresa = E.NIPC
        WHERE S.Num_servico = @ServicoID";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            label7.Text = "Nome: " + reader["Nproprio"].ToString();
                            label10.Text = "Email: " + reader["Email"].ToString();
                            label9.Text = "Telefone: " + reader["Telefone"].ToString();
                            label8.Text = "Especializacao: " + reader["Especializacao"].ToString();
                            label11.Text = "Rating médio: " +
                                (reader["Media_rating"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["Media_rating"]).ToString("0.00")
                                    : "Sem avaliações");

                            label13.Text = "Empresa: " +
                                (reader["Nome_empresa"] != DBNull.Value
                                    ? reader["Nome_empresa"].ToString()
                                    : "Sem empresa");

                            panel3.Visible = true;
                        }
                        else
                        {
                            panel3.Visible = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao obter dados do profissional: " + ex.Message);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um pedido para cancelar.");
                return;
            }

            int idPedido = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["ID_pedido"].Value);

            var confirmar = MessageBox.Show("Tem a certeza que deseja cancelar este pedido?", "Confirmar cancelamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = "DELETE FROM PedidoServico WHERE ID_pedido = @idPedido AND Estado = 'Pendente'";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@idPedido", idPedido);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        MessageBox.Show("Pedido cancelado com sucesso.");
                        CarregarPedidosPorEstado("Pendente");
                    }
                    else
                    {
                        MessageBox.Show("Erro ao cancelar o pedido. Verifique se ainda está pendente.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }
    }
}

