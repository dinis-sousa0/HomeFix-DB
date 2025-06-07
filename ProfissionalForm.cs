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
            profissionalID = DataLoader.ObterUtilizadorIDPorEmail(emailUtilizador);
            textBox3.Text = profissionalID.ToString();
            comboBox2.Items.AddRange(new string[] { "Todos", "Pagos", "Não pagos" });
            comboBox2.SelectedIndex = 0; // Seleciona "Todos" por padrão
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            decimal? rating = DataLoader.ObterRatingProfissional(profissionalID);


            if (rating.HasValue)
            {
                label9.Text = $"O meu rating: {rating.Value:0.0}";
            }
            else
            {
                label9.Text = "O meu rating: Sem avaliações";
            }

            // Opcional: carregar dados iniciais em outras abas se desejar
        }


        public void CarregarPedidosEmProgresso(int profissionalID)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = new SqlCommand("spCarregarPedidosEmProgresso", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);

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
                CarregarPedidosEmProgresso(profissionalID); // carrega os pedidos em progresso do profissional
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

            using (SqlCommand cmd = new SqlCommand("sp_ObterEspecializacaoProfissional", DatabaseHelper.GetConnection()))
            {
                cmd.CommandType = CommandType.StoredProcedure;
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

            int pedidoID = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["ID_pedido"].Value);

            try
            {
                if (!DatabaseHelper.VerifyConnection())
                {
                    MessageBox.Show("Erro na conexão com a base de dados.");
                    return;
                }

                using (SqlCommand cmd = new SqlCommand("spAceitarPedido", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@PedidoID", pedidoID);
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);

                    var outputParam = new SqlParameter("@NovoServicoID", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    int novoServicoID = (int)outputParam.Value;

                    MessageBox.Show("Pedido aceito com sucesso!");
                    CarregarPedidosPendentes(); // Atualiza lista
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao aceitar pedido: " + ex.Message);
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
                    DataGridViewRow row = dataGridView1.SelectedRows[0];

                    int servicoID = Convert.ToInt32(row.Cells["Num_servico"].Value);
                    string sumario = row.Cells["Sumario"].Value?.ToString();

                    object custoParam;
                    string custoStr = row.Cells["Custo"].Value?.ToString();

                    if (string.IsNullOrWhiteSpace(custoStr))
                    {
                        custoParam = DBNull.Value;
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

                    using (SqlCommand cmd = new SqlCommand("spAtualizarServico", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@NumServico", servicoID);
                        cmd.Parameters.AddWithValue("@Sumario", (object)sumario ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Custo", custoParam);

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Alteração salva com sucesso.");
                    CarregarPedidosEmProgresso(profissionalID);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro ao atualizar serviço: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
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

                // Usando a stored procedure que retorna empresas com rating
                using (SqlCommand cmd = new SqlCommand("sp_ObterEmpresasComRating", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    DataTable dt = new DataTable();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }

                    dataGridView3.DataSource = dt;

                    comboBox1.DisplayMember = "Nome_empresa";
                    comboBox1.ValueMember = "NIPC";
                    comboBox1.DataSource = dt;

                    comboBox1.SelectedIndexChanged -= comboBox1_SelectedIndexChanged;
                    comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;

                    AtualizarLabelRating();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar empresas: " + ex.Message);
            }
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AtualizarLabelRating();
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

                using (SqlCommand cmd = new SqlCommand("sp_ObterPedidosConcluidosProf", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);
                    cmd.Parameters.AddWithValue("@Filtro", filtro);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView4.DataSource = dt;

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

                if (!int.TryParse(textBox5.Text.Trim(), out int nipc))
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

                using (SqlCommand cmd = new SqlCommand("sp_InserirEmpresa", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NIPC", nipc);
                    cmd.Parameters.AddWithValue("@Nome_empresa", nome);
                    cmd.Parameters.AddWithValue("@Endereco", string.IsNullOrEmpty(endereco) ? DBNull.Value : (object)endereco);
                    cmd.Parameters.AddWithValue("@Telefone", telefone);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Empresa registada com sucesso!");
                CarregarEmpresas();
            }
            catch (SqlException ex)
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

                using (SqlCommand cmd = new SqlCommand("sp_AssociarProfissionalEmpresa", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NIPC", nipc);
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Associado à empresa com sucesso!");
                }
            }
            catch (SqlException ex)
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

                using (SqlCommand cmd = new SqlCommand("sp_DesvincularProfissional", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProfissionalID", profissionalID);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Profissional agora é independente.");
                }
            }
            catch (SqlException ex)
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

            try
            {
                using (SqlCommand cmd = new SqlCommand("sp_ConcluirPedidoPorServico", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Pedido concluído com sucesso!");
                    CarregarPedidosPendentes();
                }
            }
            catch (SqlException ex)
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
        private void AtualizarLabelRating()
        {
            if (comboBox1.SelectedItem == null) return;

            DataRowView selectedRow = comboBox1.SelectedItem as DataRowView;
            if (selectedRow != null)
            {
                decimal rating = 0;
                decimal.TryParse(selectedRow["rating_empregados"].ToString(), out rating);
                label8.Text = $"Rating: {rating:F2}";
            }
        }

    }
}
