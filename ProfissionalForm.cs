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
            // Supondo que a aba dos pedidos pendentes é a segunda aba (index 1)
            if (tabControl1.SelectedTab == tabPage2)
            {
                //CarregarServicosPendentes();
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

                string sql = @"SELECT ID_pedido, Localizacao, data_pedido, Descricao, Estado
                       FROM PedidoServico
                       WHERE Estado = 'Pendente'";

                if (!string.IsNullOrWhiteSpace(filtroLocalizacao))
                {
                    sql += " AND Localizacao LIKE @filtro";
                }

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    if (!string.IsNullOrWhiteSpace(filtroLocalizacao))
                    {
                        cmd.Parameters.AddWithValue("@filtro", "%" + filtroLocalizacao + "%");
                    }

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
    }
}
