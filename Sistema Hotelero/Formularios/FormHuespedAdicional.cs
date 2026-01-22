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
    public partial class FormHuespedAdicional: Form
    {
        private Conexion mConexion;
        int idReserva;
        public FormHuespedAdicional(int idReserva)
        {
            InitializeComponent();
            mConexion = new Conexion();
            this.idReserva = idReserva;
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (txtNombre.Text == "" || txtApellido.Text == "" || txtDireccion.Text == "" || txtDocumento.Text == "" || txtTelefono.Text == "")
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = mConexion.getConexion())
            {

                //Insertar huesped principal
                MySqlCommand cmdHuesped = new MySqlCommand(@"INSERT INTO huesped(nombre, apellido, documento, direccion, telefono) VALUES
                                                              (@n, @a, @d, @dir, @t); SELECT LAST_INSERT_ID();", conn);
                cmdHuesped.Parameters.AddWithValue("@n", txtNombre.Text);
                cmdHuesped.Parameters.AddWithValue("@a", txtApellido.Text);
                cmdHuesped.Parameters.AddWithValue("@d", txtDocumento.Text);
                cmdHuesped.Parameters.AddWithValue("@dir", txtDireccion.Text);
                cmdHuesped.Parameters.AddWithValue("@t", txtTelefono.Text);
                int idHuesped = Convert.ToInt32(cmdHuesped.ExecuteScalar());

                //Insertar relacion detalle_reserva
                MySqlCommand cmdDetalle = new MySqlCommand(@"INSERT INTO detalle_reserva (idHuesped, idReserva, esPrincipal) VALUES 
                                                          (@h, @r, @ep);", conn);
                cmdDetalle.Parameters.AddWithValue("@h", idHuesped);
                cmdDetalle.Parameters.AddWithValue("@r", idReserva);
                cmdDetalle.Parameters.AddWithValue("@ep", 0);
                cmdDetalle.ExecuteNonQuery();

                MessageBox.Show("Huésped cargado Correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
    }
}
