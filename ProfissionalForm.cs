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
    public partial class ProfissionlForm : Form
    {
        private string emailUtilizador;
        private int profissionalID;
        public ProfissionlForm(string email)
        {
            InitializeComponent();
            emailUtilizador = email;
            profissionalID = ObterProfissionalIDPorEmail(emailUtilizador);
            textBox3.Text = profissionalID.ToString();
            comboBox2.Items.AddRange(new string[] { "Todos", "Pagos", "Não pagos" });
            comboBox2.SelectedIndex = 0; // Seleciona "Todos" por padrão
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            // Opcional: carregar dados iniciais em outras abas se desejar
        }

        private int ObterProfissionalIDPorEmail(string email)
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
                    throw new Exception("Profissional não encontrado.");
            }
        }

        private void CarregarPedidosEmProgresso()
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = @"
            SELECT ps.ID_pedido, ps.Localizacao, ps.data_pedido, ps.Descricao, ps.Estado,
                   s.Num_servico, s.Sumario, s.Custo
            FROM PedidoServico ps
            JOIN Servico s ON ps.Servico = s.Num_servico
            WHERE ps.Estado = 'Progresso' AND s.Profissional = @profissionalID";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@profissionalID", profissionalID);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView1.DataSource = dt;

                        // Permitir edição direta de Sumario e Custo
                        dataGridView1.Columns["Sumario"].ReadOnly = false;
                        dataGridView1.Columns["Custo"].ReadOnly = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos em progresso: " + ex.Message);
            }
        }



        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filtro = textBox1.Text.Trim();
            CarregarPedidosPendentes(filtro);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage2)
            {
                CarregarPedidosEmProgresso(); // carrega os pedidos em progresso do profissional
            }
            else if (tabControl1.SelectedTab == tabPage3)
            {
                // Carrega pedidos concluídos com filtro padrão (Todos)
                CarregarPedidosConcluidos(comboBox2.SelectedItem?.ToString() ?? "Todos");
            }
            else if (tabControl1.SelectedTab == tabPage4)
            {
                CarregarEmpresas();
            }
        }


        private string ObterEspecializacaoProfissional(int profissionalID)
        {
            if (!DatabaseHelper.VerifyConnection())
                throw new Exception("Erro na conexão com a base de dados.");

            string sql = "SELECT Espec FROM Profissional WHERE ID = @ProfissionalID";

            using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);
                object result = cmd.ExecuteScalar();
                if (result != null)
                    return result.ToString();
                else
                    throw new Exception("Especialização do profissional não encontrada.");
            }
        }

        private void CarregarPedidosPendentes(string filtroLocalizacao = "")
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string especializacao = ObterEspecializacaoProfissional(profissionalID);

                using (SqlCommand cmd = new SqlCommand("sp_ObterPedidosPendentes", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Especializacao", especializacao);

                    if (string.IsNullOrWhiteSpace(filtroLocalizacao))
                        cmd.Parameters.AddWithValue("@FiltroLocalizacao", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@FiltroLocalizacao", filtroLocalizacao);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView2.DataSource = dt;
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
            //CarregarServicosPendentes();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {
            CarregarPedidosPendentes();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, selecione um pedido para aceitar.");
                return;
            }

            // Obtem o ID do pedido selecionado
            int pedidoID = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["ID_pedido"].Value);

            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                using (SqlConnection cn = DatabaseHelper.GetConnection())
                {
                    using (SqlTransaction transaction = cn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Criar novo Serviço (Sumario e Custo NULL)
                            string insertServico = @"INSERT INTO Servico (Sumario, Custo, Profissional) 
                                             VALUES (NULL, NULL, @profissionalID);
                                             SELECT CAST(SCOPE_IDENTITY() AS INT);";

                            int novoServicoID;
                            using (SqlCommand cmdInsertServico = new SqlCommand(insertServico, cn, transaction))
                            {
                                cmdInsertServico.Parameters.AddWithValue("@profissionalID", profissionalID);
                                novoServicoID = (int)cmdInsertServico.ExecuteScalar();
                            }

                            // 2. Atualizar pedido para associar serviço e mudar estado
                            string updatePedido = @"UPDATE PedidoServico 
                                            SET Estado = 'Progresso', Servico = @servicoID
                                            WHERE ID_pedido = @pedidoID";

                            using (SqlCommand cmdUpdatePedido = new SqlCommand(updatePedido, cn, transaction))
                            {
                                cmdUpdatePedido.Parameters.AddWithValue("@servicoID", novoServicoID);
                                cmdUpdatePedido.Parameters.AddWithValue("@pedidoID", pedidoID);
                                int linhasAfetadas = cmdUpdatePedido.ExecuteNonQuery();

                                if (linhasAfetadas == 0)
                                    throw new Exception("Pedido não encontrado ou já foi aceito.");
                            }

                            transaction.Commit();

                            MessageBox.Show("Pedido aceito com sucesso!");
                            CarregarPedidosPendentes();  // Recarregar lista para atualizar os pedidos pendentes
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show("Erro ao aceitar pedido: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            this.Close(); // Fecha o form atual (importante para libertar memória)
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, selecione um pedido para atualizar.");
                return;
            }

            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                using (SqlConnection cn = DatabaseHelper.GetConnection())
                {
                    // Pega a primeira linha selecionada (podes adaptar para múltiplas se quiseres)
                    DataGridViewRow row = dataGridView1.SelectedRows[0];

                    int servicoID = Convert.ToInt32(row.Cells["Num_servico"].Value);
                    string sumario = row.Cells["Sumario"].Value?.ToString();

                    object custoParam;
                    string custoStr = row.Cells["Custo"].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(custoStr))
                    {
                        custoParam = DBNull.Value; // NULL no banco
                    }
                    else if (decimal.TryParse(custoStr, out decimal custo))
                    {
                        custoParam = custo;
                    }
                    else
                    {
                        MessageBox.Show("Custo inválido na linha " + row.Index);
                        return;
                    }

                    string sql = @"UPDATE Servico SET Sumario = @Sumario, Custo = @Custo WHERE Num_servico = @ServicoID";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@Sumario", (object)sumario ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Custo", custoParam);
                        cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Alteração salva com sucesso.");
                    CarregarPedidosEmProgresso(); // Atualiza a grid para mostrar o valor correto
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao atualizar serviço: " + ex.Message);
            }
        }

        private void CarregarEmpresas()
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = "SELECT NIPC, Nome_empresa, Endereço, Telefone, rating_empregados FROM Empresa";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                    dataGridView3.DataSource = dt;

                    comboBox1.DisplayMember = "Nome_empresa";
                    comboBox1.ValueMember = "NIPC";
                    comboBox1.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar empresas: " + ex.Message);
            }
        }

        private void CarregarPedidosConcluidos(string filtro = "Todos")
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                // SQL base que traz pedidos concluídos
                string sql = @"
            SELECT ps.ID_pedido, ps.Localizacao, ps.data_pedido, ps.Descricao, ps.Estado,
                   s.Num_servico, s.Sumario, s.Custo,
                   CASE WHEN p.ID_transacao IS NOT NULL THEN 'Pago' ELSE 'Não pago' END AS StatusPagamento
            FROM PedidoServico ps
            JOIN Servico s ON ps.Servico = s.Num_servico
            LEFT JOIN Pagamento p ON s.Num_servico = p.Servico
            WHERE ps.Estado = 'Concluido' AND s.Profissional = @ProfissionalID";

                // Filtra dependendo da escolha
                if (filtro == "Pagos")
                {
                    sql += " AND p.ID_transacao IS NOT NULL";
                }
                else if (filtro == "Não pagos")
                {
                    sql += " AND p.ID_transacao IS NULL";
                }
                // se for "Todos" não adiciona filtro extra

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    // Add the parameter here
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView4.DataSource = dt;

                        // Opcional: ajustar colunas para exibir bem
                        dataGridView4.Columns["StatusPagamento"].HeaderText = "Status do Pagamento";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos concluídos: " + ex.Message);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                int nipc;
                if (!int.TryParse(textBox5.Text.Trim(), out nipc))
                {
                    MessageBox.Show("NIPC inválido.");
                    return;
                }

                string nome = textBox4.Text.Trim();
                string endereco = textBox2.Text.Trim();
                string telefone = textBox6.Text.Trim();

                if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(telefone))
                {
                    MessageBox.Show("Nome e Telefone são obrigatórios.");
                    return;
                }


                string sql = @"INSERT INTO Empresa (NIPC, Nome_empresa, Endereço, Telefone) 
                       VALUES (@NIPC, @Nome, @Endereco, @Telefone)";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@NIPC", nipc);
                    cmd.Parameters.AddWithValue("@Nome", nome);
                    cmd.Parameters.AddWithValue("@Endereco", (object)endereco ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Telefone", telefone);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Empresa registada com sucesso!");
                CarregarEmpresas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao registar empresa: " + ex.Message);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedValue == null)
            {
                MessageBox.Show("Por favor, selecione uma empresa.");
                return;
            }

            int nipc = (int)comboBox1.SelectedValue;

            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = @"UPDATE Profissional SET Empresa = @NIPC WHERE ID = @ProfissionalID";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@NIPC", nipc);
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                        MessageBox.Show("Associado à empresa com sucesso!");
                    else
                        MessageBox.Show("Profissional não encontrado ou erro na associação.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao associar empresa: " + ex.Message);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                string sql = @"UPDATE Profissional SET Empresa = NULL WHERE ID = @ProfissionalID";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                        MessageBox.Show("Profissional agora é independente.");
                    else
                        MessageBox.Show("Erro ao atualizar status do profissional.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao atualizar profissional: " + ex.Message);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um pedido.");
                return;
            }

            DataGridViewRow row = dataGridView1.SelectedRows[0];

            if (row.Cells["Num_servico"].Value == DBNull.Value)
            {
                MessageBox.Show("Este pedido ainda não tem serviço associado.");
                return;
            }

            int servicoID = Convert.ToInt32(row.Cells["Num_servico"].Value);
            string custoStr = row.Cells["Custo"].Value?.ToString();

            if (string.IsNullOrWhiteSpace(custoStr))
            {
                MessageBox.Show("O custo ainda não foi definido. Não é possível concluir.");
                return;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand(@"UPDATE PedidoServico 
                                                 SET Estado = 'Concluido' 
                                                 WHERE Servico = @ServicoID", DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                        MessageBox.Show("Pedido concluído com sucesso!");
                    else
                        MessageBox.Show("Erro ao concluir pedido.");

                    CarregarPedidosPendentes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao concluir: " + ex.Message);
            }
        }

        private void dataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filtroSelecionado = comboBox2.SelectedItem.ToString();
            CarregarPedidosConcluidos(filtroSelecionado);
        }
    }
}
