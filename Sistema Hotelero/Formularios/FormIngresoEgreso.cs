using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sistema_Hotelero.Formularios
{
    public partial class FormIngresoEgreso: Form
    {
        private string estadoActual;
        private Conexion conexion;

        public FormIngresoEgreso()
        {
            InitializeComponent();
            conexion = new Conexion();
        }

        public FormIngresoEgreso(string estado):this()
        {
            estadoActual = estado;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (estadoActual == "INGRESO")
            {
                using (MySqlConnection conn = conexion.getConexion())
                {
                    string queryIngreso = "INSERT INTO finanzas(fecha, concepto, tipo, monto) VALUES (@f, @c, @t, @m)";
                    MySqlCommand cmdIngreso = new MySqlCommand(queryIngreso, conn);
                    cmdIngreso.Parameters.AddWithValue("@f",DateTime.Now);
                    cmdIngreso.Parameters.AddWithValue("@c", txtConcepto.Text);
                    cmdIngreso.Parameters.AddWithValue("@t", estadoActual);
                    cmdIngreso.Parameters.AddWithValue("@m", Convert.ToInt32(txtMonto.Text));
                    cmdIngreso.ExecuteNonQuery();
                }
            }
            else if (estadoActual == "EGRESO")
            {
                using (MySqlConnection conn = conexion.getConexion())
                {
                    string queryEgreso = "INSERT INTO finanzas(fecha, concepto, tipo, monto) VALUES (@f, @c, @t, @m)";
                    MySqlCommand cmdEgreso = new MySqlCommand(queryEgreso, conn);
                    cmdEgreso.Parameters.AddWithValue("@f", DateTime.Now);
                    cmdEgreso.Parameters.AddWithValue("@c", txtConcepto.Text);
                    cmdEgreso.Parameters.AddWithValue("@t", estadoActual);
                    cmdEgreso.Parameters.AddWithValue("@m", Convert.ToInt32(txtMonto.Text));
                    cmdEgreso.ExecuteNonQuery();
                }
            }

            this.Close();
        }
    }
}
