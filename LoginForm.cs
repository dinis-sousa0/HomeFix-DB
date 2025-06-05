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
using homefix.Helpers;  // Importa o helper


namespace homefix
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            // Opcional: só abre conexão para teste na carga do form
            if (!DatabaseHelper.VerifyConnection())
            {
                MessageBox.Show("Erro ao conectar à base de dados.");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string email = textBox1.Text.Trim();
            string password = textBox2.Text;

            if (!DatabaseHelper.VerifyConnection())
            {
                label3.ForeColor = Color.Red;
                label3.Text = "Erro na conexão com a base de dados.";
                return;
            }

            try
            {
                SqlConnection cn = DatabaseHelper.GetConnection();

                // Verifica credenciais
                string sql = "SELECT ID_Utilizador FROM Utilizador WHERE Email = @Email AND PassHash = @PassHash";
                int utilizadorId = -1;

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PassHash", password); // Em produção, use hash

                    object result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int id))
                    {
                        utilizadorId = id;
                    }
                    else
                    {
                        label3.ForeColor = Color.Red;
                        label3.Text = "Email ou senha inválidos.";
                        return;
                    }
                }

                // Verifica se é profissional
                string sqlProf = "SELECT COUNT(*) FROM Profissional WHERE ID = @ID";
                bool isProfissional = false;

                using (SqlCommand cmd = new SqlCommand(sqlProf, cn))
                {
                    cmd.Parameters.AddWithValue("@ID", utilizadorId);
                    isProfissional = (int)cmd.ExecuteScalar() > 0;
                }

                // Abre o formulário correto
                this.Hide();

                if (isProfissional)
                {
                    ProfissionlForm profForm = new ProfissionlForm(email);
                    profForm.ShowDialog();
                    this.Show();  // Quando voltar, mostra o login de novo
                }
                else
                {
                    PedidoForm pedidoForm = new PedidoForm(email);
                    pedidoForm.ShowDialog();
                    this.Show();  // Quando voltar, mostra o login de novo
                }
            }
            catch (Exception ex)
            {
                label3.ForeColor = Color.Red;
                label3.Text = "Erro: " + ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm();
            registerForm.ShowDialog(); // Abre o formulário de cadastro como modal (bloqueia o Form1 até fechar)
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
