using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using homefix.Helpers;

namespace homefix
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            label7.Visible = false;
            comboBox1.Visible = false;
            panel1.Visible = false;
            radioButton1.Checked = true;

            DataLoader.CarregarEspecializacoes(comboBox1); // ← carregar comboBox1 ao iniciar o formulário
        }

       

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                panel1.Visible = false;
                label7.Visible = false;
                comboBox1.Visible = false;
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                panel1.Visible = true;
                label7.Visible = true;
                comboBox1.Visible = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!DatabaseHelper.VerifyConnection())
            {
                MessageBox.Show("Erro ao conectar à base de dados.");
                return;
            }

            string pNome = textBox1.Text.Trim();
            string uNome = textBox2.Text.Trim();
            string telefone = textBox3.Text.Trim();
            string senha = textBox4.Text.Trim();
            string morada = textBox5.Text.Trim();
            string email = textBox6.Text.Trim();

            if (string.IsNullOrEmpty(pNome) || string.IsNullOrEmpty(uNome) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Preencha todos os campos obrigatórios.");
                return;
            }

            if (!ValidationHelper.IsValidEmail(email))
            {
                MessageBox.Show("Email inválido");
                return;
            }

            if (!ValidationHelper.IsValidPhone(telefone))
            {
                MessageBox.Show("Telefone inválido");
                return;
            }

            if (radioButton2.Checked && comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecione a especialização do profissional.");
                return;
            }

            try
            {
                using (SqlCommand cmd = new SqlCommand("spRegistarUtilizador", DatabaseHelper.GetConnection()))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@pNome", pNome);
                    cmd.Parameters.AddWithValue("@uNome", uNome);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@telefone", telefone);
                    cmd.Parameters.AddWithValue("@senha", senha);
                    cmd.Parameters.AddWithValue("@morada", morada);

                    if (radioButton1.Checked)
                    {
                        cmd.Parameters.AddWithValue("@tipo", "cliente");
                        cmd.Parameters.AddWithValue("@Especializacao", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Empresa", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@tipo", "profissional");
                        cmd.Parameters.AddWithValue("@Especializacao", comboBox1.SelectedItem?.ToString() ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Empresa", DBNull.Value); // ou o valor da empresa, se aplicável
                    }

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Registo efetuado com sucesso!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }
    }
}