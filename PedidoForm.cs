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
                        // Mudar para a aba de pedidos pendentes
                        tabControl1.SelectedTab = tabPage2;

                        // Recarregar os pedidos pendentes
                        CarregarServicosPendentes();
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
            // Supondo que a aba dos pedidos pendentes é a segunda aba (index 1)
            if (tabControl1.SelectedTab == tabPage2)
            {
                CarregarServicosPendentes();
            }
        }

        private void CarregarServicosPendentes()
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
                               WHERE Cliente = @clienteID AND Estado = 'Pendente'";

                using (SqlCommand cmd = new SqlCommand(sql, DatabaseHelper.GetConnection()))
                {
                    cmd.Parameters.AddWithValue("@clienteID", clienteID);

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dataGridView1.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar pedidos pendentes: " + ex.Message);
            }
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {
            CarregarServicosPendentes();
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
    }
}
