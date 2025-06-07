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
                using (SqlConnection cn = DatabaseHelper.GetConnection())
                using (SqlCommand cmd = new SqlCommand("spLogin", cn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PassHash", password);

                    // Parâmetros OUTPUT
                    var paramID = new SqlParameter("@ID_Utilizador", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(paramID);

                    var paramTipo = new SqlParameter("@Tipo", SqlDbType.NVarChar, 20)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(paramTipo);

                    cmd.ExecuteNonQuery();

                    if (paramID.Value == DBNull.Value)
                    {
                        label3.ForeColor = Color.Red;
                        label3.Text = "Email ou senha inválidos.";
                        return;
                    }

                    int utilizadorId = (int)paramID.Value;
                    string tipo = paramTipo.Value.ToString();

                    this.Hide();

                    if (tipo == "profissional")
                    {
                        ProfissionlForm profForm = new ProfissionlForm(email);
                        profForm.ShowDialog();
                        this.Show();
                    }
                    else if (tipo == "cliente")
                    {
                        PedidoForm pedidoForm = new PedidoForm(email);
                        pedidoForm.ShowDialog();
                        this.Show();
                    }
                    else
                    {
                        label3.ForeColor = Color.Red;
                        label3.Text = "Tipo de utilizador desconhecido.";
                        this.Show();
                    }
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

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
