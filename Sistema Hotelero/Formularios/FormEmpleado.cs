using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sistema_Hotelero.Formularios
{
    public partial class FormEmpleado: Form
    {
        private Conexion conexion;
        private int? idEmpleado = null;

        public FormEmpleado()
        {
            InitializeComponent();
            conexion = new Conexion();
        }

        public FormEmpleado(int id) : this()
        {
            idEmpleado = id;
        }

        private void FormEmpleado_Load(object sender, EventArgs e)
        {
            if (idEmpleado != null)
            {
                CargarDatosEmpleado();
            }
        }

        private void CargarDatosEmpleado()
        {
            using (MySqlConnection conn = conexion.getConexion())
            {
                string query = "SELECT * FROM personal WHERE ID = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idEmpleado);

                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtNombre.Text = reader["nombre"].ToString();
                    txtApellido.Text = reader["apellido"].ToString();
                    txtDocumento.Text = reader["documento"].ToString();
                    txtTelefono.Text = reader["telefono"].ToString();
                    txtSueldo.Text = reader["sueldo"].ToString();
                }
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            using(MySqlConnection conn = conexion.getConexion())
            {
                if (idEmpleado == null)
                {
                    // --- Nuevo empleado ---
                    string query = @"INSERT INTO personal (nombre, apellido, documento, telefono, sueldo, fechaPago, fechaAlta)
                                     VALUES (@nombre, @apellido, @documento, @telefono, @sueldo, @fechaPago, @fechaAlta)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@apellido", txtApellido.Text);
                    cmd.Parameters.AddWithValue("@documento", txtDocumento.Text);
                    cmd.Parameters.AddWithValue("@telefono", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@sueldo", Convert.ToDecimal(txtSueldo.Text));
                    cmd.Parameters.AddWithValue("@fechaPago", DateTime.Now.AddMonths(1));
                    cmd.Parameters.AddWithValue("@fechaAlta", DateTime.Now);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Empleado agregado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // --- Actualizar empleado existente ---
                    string query = @"UPDATE personal 
                                     SET nombre=@nombre, apellido=@apellido, documento=@documento, telefono=@telefono, sueldo=@sueldo 
                                     WHERE ID=@id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@apellido", txtApellido.Text);
                    cmd.Parameters.AddWithValue("@documento", txtDocumento.Text);
                    cmd.Parameters.AddWithValue("@telefono", txtTelefono.Text);
                    cmd.Parameters.AddWithValue("@sueldo", Convert.ToDecimal(txtSueldo.Text));
                    cmd.Parameters.AddWithValue("@id", idEmpleado);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Empleado actualizado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                this.Close();
            }
        }
    }
}
