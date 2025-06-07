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
            clienteID = DataLoader.ObterUtilizadorIDPorEmail(emailUtilizador);
            textBox3.Text = clienteID.ToString();
            CarregarTiposPagamento();
            DataLoader.CarregarEspecializacoes(comboBox2);
            panel1.Visible = false;
            panel2.Visible = false;
            panel4.Visible = false;

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

                using (SqlCommand cmd = new SqlCommand("spCarregarTiposPagamento", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

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
                CarregarPedidosConcluidos(clienteID); // Novo método específico
            }
            else if (tabControl1.SelectedTab == tabPage7) // Newsletter (nova aba)
            {
                CarregarStatusNewsletter();
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

        public void CarregarPedidosConcluidos(int clienteID)
        {
            try
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    // Pedidos por pagar (sem pagamento)
                    using (SqlCommand cmdPorPagar = new SqlCommand("spPedidosPorPagar", conn))
                    {
                        cmdPorPagar.CommandType = CommandType.StoredProcedure;
                        cmdPorPagar.Parameters.AddWithValue("@ClienteID", clienteID);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmdPorPagar))
                        {
                            DataTable dtPorPagar = new DataTable();
                            adapter.Fill(dtPorPagar);
                            dataGridView3.DataSource = dtPorPagar;
                        }
                    }

                    // Pedidos concluídos (com pagamento)
                    using (SqlCommand cmdConcluidos = new SqlCommand("spPedidosConcluidos", conn))
                    {
                        cmdConcluidos.CommandType = CommandType.StoredProcedure;
                        cmdConcluidos.Parameters.AddWithValue("@ClienteID", clienteID);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmdConcluidos))
                        {
                            DataTable dtConcluidos = new DataTable();
                            adapter.Fill(dtConcluidos);
                            dataGridView4.DataSource = dtConcluidos;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos concluídos: " + ex.Message);
            }
        }


        private void button3_Click(object sender, EventArgs e)
        {
            this.Close(); // Fecha o form atual (importante para libertar memória)
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView4.SelectedRows.Count == 0)
            {
                MessageBox.Show("Selecione um pedido primeiro.");
                return;
            }

            int idPedido = Convert.ToInt32(dataGridView4.SelectedRows[0].Cells["ID_pedido"].Value);
            string comentario = textBox5.Text.Trim();

            if (!decimal.TryParse(textBox4.Text, out decimal rating) || rating < 0 || rating > 5)
            {
                MessageBox.Show("Insira um rating válido (0-5).");
                return;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand("spInserirAvaliacao", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_pedido", idPedido);
                    cmd.Parameters.AddWithValue("@Comentario", comentario);
                    cmd.Parameters.AddWithValue("@Rating", rating);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Avaliação enviada com sucesso!");
                    panel1.Visible = false;
                    textBox5.Clear();
                    textBox4.Clear();
                    dataGridView4_SelectionChanged(dataGridView4, EventArgs.Empty);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }


        private void dataGridView3_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView3.SelectedRows.Count == 0)
            {
                panel2.Visible = false;
                return;
            }

            int servicoID = Convert.ToInt32(dataGridView3.SelectedRows[0].Cells["Servico"].Value);

            if (!DatabaseHelper.VerifyConnection())
            {
                MessageBox.Show("Erro de ligação à base de dados.");
                return;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand("spVerificarPagamento", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ServicoID", servicoID);
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);

                    SqlParameter outputParam = new SqlParameter("@PagamentoExiste", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    bool pagamentoExiste = (bool)outputParam.Value;

                    if (pagamentoExiste)
                    {
                        panel2.Visible = false;
                        MessageBox.Show("Pagamento já foi efetuado para este serviço.");
                    }
                    else
                    {
                        panel2.Visible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
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

            string resultado = DataLoader.RegistarPagamento(servicoID, clienteID, sumario, tipoNome);
            MessageBox.Show(resultado);

            if (resultado == "Pagamento registado com sucesso!")
            {
                panel2.Visible = false;
                CarregarPedidosConcluidos(clienteID);
            }
        }


        private void dataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView2.SelectedRows.Count == 0)
            {
                panel3.Visible = false;
                return;
            }

            int servicoID = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["Servico"].Value);

            if (DataLoader.ObterDadosProfissionalPorServico(servicoID,
                out string nome, out string email, out string telefone, out string especializacao,
                out string rating, out string empresa))
            {
                label7.Text = nome;
                label10.Text = email;
                label9.Text = telefone;
                label8.Text = especializacao;
                label11.Text = rating;
                label13.Text = empresa;

                panel3.Visible = true;
            }
            else
            {
                panel3.Visible = false;
            }
        }

        private void dataGridView4_SelectionChanged(object sender, EventArgs e)
        {
            if (tabControl2.SelectedTab == tabPage6 && dataGridView4.SelectedRows.Count > 0)
            {
                int idPedido = Convert.ToInt32(dataGridView4.SelectedRows[0].Cells["ID_pedido"].Value);

                try
                {
                    if (!DatabaseHelper.VerifyConnection())
                    {
                        MessageBox.Show("Erro na conexão com a base de dados.");
                        panel1.Visible = false;
                        return;
                    }

                    using (SqlCommand cmd = new SqlCommand("spObterAvaliacaoPorPedido", DatabaseHelper.GetConnection()))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ID_pedido", idPedido);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bool existeAvaliacao = Convert.ToInt32(reader["ExisteAvaliacao"]) == 1;

                                if (!existeAvaliacao)
                                {
                                    panel1.Visible = true;
                                    panel4.Visible = false;
                                    textBox5.Clear();
                                    textBox4.Clear();
                                }
                                else
                                {
                                    panel1.Visible = false;
                                    panel4.Visible = true;

                                    decimal estrelas = reader["Rating"] != DBNull.Value ? Convert.ToDecimal(reader["Rating"]) : 0m;
                                    string comentario = reader["Comentario"] != DBNull.Value ? reader["Comentario"].ToString() : "";

                                    label16.Text = $"Avaliado em {estrelas} Estrela(s)";
                                    textBox7.Text = comentario;
                                }
                            }
                            else
                            {
                                panel1.Visible = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro: " + ex.Message);
                    panel1.Visible = false;
                }
            }
            else
            {
                panel1.Visible = false;
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

                using (SqlCommand cmd = new SqlCommand("spCancelarPedido", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_pedido", idPedido);

                    var outputParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    int rows = (int)outputParam.Value;

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


        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView4.SelectedRows.Count == 0)
                    return;

                int idPedido = Convert.ToInt32(dataGridView4.SelectedRows[0].Cells["ID_pedido"].Value);

                // Confirmação do usuário
                DialogResult confirm = MessageBox.Show(
                    "Tem a certeza que deseja apagar esta avaliação?",
                    "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question
                );

                if (confirm == DialogResult.No)
                    return;

                using (SqlCommand cmd = new SqlCommand("spEliminarAvaliacaoPorPedido", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_pedido", idPedido);

                    SqlParameter outputParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    int rowsAffected = (int)outputParam.Value;

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Avaliação eliminada com sucesso.");

                        // Atualiza UI
                        panel4.Visible = false;
                        label16.Text = "";
                        textBox5.Clear();

                        // Forçar atualização da seleção para recarregar dados
                        dataGridView4_SelectionChanged(dataGridView4, EventArgs.Empty);
                    }
                    else
                    {
                        MessageBox.Show("Nenhuma avaliação encontrada para eliminar.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }
        private void CarregarStatusNewsletter()
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("spObterStatusNewsletter", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);

                    SqlParameter outputParam = new SqlParameter("@ReceberNewsletter", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    object result = outputParam.Value;

                    if (result != DBNull.Value && result != null)
                    {
                        bool status = (bool)result;
                        checkBox1.Checked = status;
                    }
                    else
                    {
                        checkBox1.Checked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar status da newsletter: " + ex.Message);
            }
        }
        private void AtualizarNewsletter()
        {
            try
            {
                int novoStatus = checkBox1.Checked ? 1 : 0;

                using (SqlCommand cmd = new SqlCommand("spAtualizarNewsletter", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClienteID", clienteID);
                    cmd.Parameters.AddWithValue("@NovoStatus", novoStatus);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Preferência de newsletter atualizada com sucesso!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao atualizar newsletter: " + ex.Message);
            }
        }
        private void buttonAtualizarNewsletter_Click(object sender, EventArgs e)
        {
            AtualizarNewsletter();
        }

    }

}

