using iTextSharp.text.pdf;
using iTextSharp.text;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sistema_Hotelero.Formularios
{
    public partial class FormEditarReserva: Form
    {
        private int idReserva;
        private Conexion mConexion;
        private int idHabitacionActual;
        private int cantHuespedesActual;

        public FormEditarReserva(int idReserva)
        {
            InitializeComponent();
            this.idReserva = idReserva;
        }

        public void FormEditarReserva_Load(object sender, EventArgs e)
        {
            mConexion = new Conexion();
            CargarHabitaciones();
            CargarDatosReserva();
            CargarHuespedes();
        }

        private void CargarHabitaciones()
        {
            using (MySqlConnection conn = mConexion.getConexion())
            {
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT ID, habitacion, camas FROM habitaciones WHERE estado = 'Disponible'",
                                                           conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                cmbHabitacion.DataSource = dt;
                cmbHabitacion.DisplayMember = "habitacion";
                cmbHabitacion.ValueMember = "ID";
            }
        }

        private void CargarHuespedes()
        {
            if (cmbHabitacion.SelectedValue == null) return;

            using (MySqlConnection conn = mConexion.getConexion())
            {
                MySqlCommand cmd = new MySqlCommand("SELECT camas FROM habitaciones WHERE ID=@id", conn);
                cmd.Parameters.AddWithValue("@id", cmbHabitacion.SelectedValue);
                int camas = Convert.ToInt32(cmd.ExecuteScalar());

                cmbCantHuespedes.Items.Clear();
                for (int i = 1; i <= camas; i++)
                {
                    cmbCantHuespedes.Items.Add(i);
                }

                cmbCantHuespedes.SelectedIndex = cantHuespedesActual;
            }
        }

        private void CargarDatosReserva()
        {
            string query = $@"SELECT r.*, h.nombre, h.apellido, h.documento, h.direccion, h.telefono FROM reservas r INNER JOIN detalle_reserva dr ON dr.idReserva = r.ID INNER JOIN 
                           huesped h ON dr.idHuesped = h.ID WHERE r.ID = {idReserva} AND dr.esPrincipal = 1;";

            DataTable datos = ObtenerDatos(query);

            if (datos.Rows.Count > 0)
            {
                var row = datos.Rows[0];
                txtNombre.Text = row["nombre"].ToString();
                txtApellido.Text = row["apellido"].ToString();
                txtDocumento.Text = row["documento"].ToString();
                txtDireccion.Text = row["direccion"].ToString();
                txtTelefono.Text = row["telefono"].ToString();
                txtVehiculo.Text = row["vehiculo"].ToString();
                txtPatente.Text = row["patente"].ToString();
                dtpIngreso.Value = Convert.ToDateTime(row["fechaIngreso"]);
                dtpEgreso.Value = Convert.ToDateTime(row["fechaEgreso"]);

                idHabitacionActual = Convert.ToInt32(row["idHabitacion"]);
                if (cmbHabitacion.DataSource != null)
                {
                    cmbHabitacion.SelectedValue = idHabitacionActual;
                }

                cantHuespedesActual = Convert.ToInt32(row["huespedes"])-1;
            }
        }

        private void cmbHabitacion_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbHabitacion.SelectedValue == null) return;

            using (MySqlConnection conn = mConexion.getConexion())
            {
                MySqlCommand cmd = new MySqlCommand("SELECT camas FROM habitaciones WHERE ID=@id", conn);
                cmd.Parameters.AddWithValue("@id", cmbHabitacion.SelectedValue);
                object result = cmd.ExecuteScalar();
                int camas = 0;
                if (result != DBNull.Value && result != null)
                {
                    camas = Convert.ToInt32(result);
                }

                cmbCantHuespedes.Items.Clear();
                for (int i = 1; i <= camas; i++)
                {
                    cmbCantHuespedes.Items.Add(i);
                }

                if (cmbCantHuespedes.Items.Count > 0)
                {
                    cmbCantHuespedes.SelectedIndex = 0;
                }
            }
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("¿Seguro que deseas cancelar esta reserva?", "Confirmar cancelación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string query = $"DELETE FROM reservas WHERE ID = {idReserva}";
                mConexion.EjecutarConsulta(query); // método que ejecute non-query en tu clase de conexión

                MessageBox.Show("Reserva cancelada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }

        private void btnImprimir_Click(object sender, EventArgs e)
        {
            GenerarPDF(idReserva);
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            string query = $@"UPDATE reservas SET fechaIngreso = '{dtpIngreso.Value:yyyy-MM-dd}', fechaEgreso = '{dtpEgreso.Value:yyyy-MM-dd}',vehiculo = '{txtVehiculo.Text}',
            patente = '{txtPatente.Text}' WHERE ID = {idReserva};";
            mConexion.EjecutarConsulta(query);

            MessageBox.Show("Reserva actualizada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }

        private void GenerarPDF(int idReserva)
        {
            try
            {
                using (MySqlConnection conn = mConexion.getConexion())
                {

                    //Obtener datos de la reserva
                    string queryReserva = @"Select r.ID, r.fechaIngreso, r.fechaEgreso, r.idHabitacion, r.vehiculo, r.patente FROM
                                            reservas r WHERE r.ID = @id;";
                    MySqlCommand cmdReserva = new MySqlCommand(queryReserva, conn);
                    cmdReserva.Parameters.AddWithValue("@id", idReserva);
                    MySqlDataReader drReserva = cmdReserva.ExecuteReader();
                    if (!drReserva.Read())
                    {
                        MessageBox.Show("No se encontró la reserva especifica.");
                        return;
                    }
                    string idHabitacion = drReserva["idHabitacion"].ToString();
                    string vehiculo = drReserva["vehiculo"].ToString();
                    string patente = drReserva["patente"].ToString();
                    DateTime ingreso = Convert.ToDateTime(drReserva["fechaIngreso"]);
                    DateTime egreso = Convert.ToDateTime(drReserva["fechaEgreso"]);
                    drReserva.Close();

                    //Obtener nombre de la habitacion
                    string habitacionNombre = "";
                    string queryHabitacion = "SELECT habitacion FROM habitaciones WHERE ID =@idHabitacion;";
                    MySqlCommand cmdHab = new MySqlCommand(queryHabitacion, conn);
                    {
                        cmdHab.Parameters.AddWithValue("@idHabitacion", idHabitacion);
                        object result = cmdHab.ExecuteScalar();
                        habitacionNombre = result != null ? result.ToString() : "(No especificada)";
                    }

                    //Obtener todos los huéspedes asociados a la reserva
                    string queryHuespedes = @"SELECT h.nombre, h.apellido, h.documento, h.direccion, h.telefono, dr.esPrincipal FROM
                                            detalle_reserva dr INNER JOIN huesped h ON dr.idHuesped = h.ID WHERE dr.idReserva = @id
                                            ORDER BY dr.esPrincipal DESC;"; //El principal primero
                    MySqlDataAdapter da = new MySqlDataAdapter(queryHuespedes, conn);

                    da.SelectCommand.Parameters.AddWithValue("@id", idReserva);
                    DataTable dtHuespedes = new DataTable();
                    da.Fill(dtHuespedes);

                    //Crear PDF
                    string fileName = $"Reserva_{idReserva}.pdf";
                    string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName);
                    Document doc = new Document(PageSize.A4, 50, 50, 40, 40);
                    PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
                    doc.Open();

                    //===ENCABEZADO DEL ALOJAMIENTO===
                    Paragraph header = new Paragraph("NOMBRE DEL HOTEL\nCUIL:30-12345467-9\nDirección: Av. Libertad 456 - Gualeguaychu -" +
                                                    " ENTRE RIOS\nTel: (03446) 432456\n\n", FontFactory.GetFont(
                                                    FontFactory.HELVETICA_BOLD, 12));
                    header.Alignment = Element.ALIGN_CENTER;
                    doc.Add(header);
                    doc.Add(new Paragraph($"Fecha de emision: {DateTime.Now:dd/MM/yyyy}\n\n", FontFactory.GetFont(
                                         FontFactory.HELVETICA, 10)));

                    //===DETALLE DE LA RESERVA===
                    PdfPTable tablaReserva = new PdfPTable(2);
                    tablaReserva.WidthPercentage = 100;
                    tablaReserva.SpacingAfter = 10f;
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Habitación:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase(habitacionNombre, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Fecha ingreso:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase(ingreso.ToString("dd/MM/yyyy"), FontFactory.GetFont(
                                                                FontFactory.HELVETICA, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Fecha egreso:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase(egreso.ToString("dd/MM/yyyy"), FontFactory.GetFont(
                                                                FontFactory.HELVETICA, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Vehículo:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase(vehiculo, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Patente:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                    { Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase(patente, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                    { Border = 0 });
                    doc.Add(tablaReserva);

                    //===HUESPEDES ENUMERADOS===

                    Paragraph tituloHuespedes = new Paragraph("Lista de huéspedes", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11));
                    tituloHuespedes.Alignment = Element.ALIGN_LEFT;
                    tituloHuespedes.SpacingBefore = 10f;
                    doc.Add(tituloHuespedes);
                    doc.Add(new Paragraph("\n"));

                    PdfPTable tablaHuespedes = new PdfPTable(6);
                    tablaHuespedes.WidthPercentage = 100;
                    tablaHuespedes.SetWidths(new float[] { 0.6f, 1.5f, 1.5f, 1.2f, 2.5f, 1.2f });

                    //Encabezados
                    string[] headers = { "N°", "Nombre", "Apellido", "Documento", "Direccion", "Teléfono" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9)))
                        { HorizontalAlignment = Element.ALIGN_CENTER, BackgroundColor = new BaseColor(240, 240, 240) };
                        tablaHuespedes.AddCell(cell);
                    }

                    //Flias con datos
                    int n = 1;
                    foreach (DataRow row in dtHuespedes.Rows)
                    {
                        tablaHuespedes.AddCell(n.ToString());
                        tablaHuespedes.AddCell(row["nombre"].ToString());
                        tablaHuespedes.AddCell(row["apellido"].ToString());
                        tablaHuespedes.AddCell(row["documento"].ToString());
                        tablaHuespedes.AddCell(row["direccion"].ToString());
                        tablaHuespedes.AddCell(row["telefono"].ToString());
                        n++;
                    }
                    doc.Add(tablaHuespedes);

                    //===PIE DE FIRMA===

                    doc.Add(new Paragraph("\n\n Firma del responsable de recepción:______________________________\n", FontFactory.GetFont
                                         (FontFactory.HELVETICA, 10)));
                    doc.Add(new Paragraph("\n Gracias por elegirnos. ¡Le deseamos una excelente estadía!", FontFactory.GetFont
                                         (FontFactory.HELVETICA_OBLIQUE, 10, BaseColor.GRAY)));
                    doc.Close();

                    MessageBox.Show($"PDF generado correctamente:\n{filePath}", "Comprobante de reserva", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el PDF: " + ex.Message);
            }
        }

        private DataTable ObtenerDatos(string query)
        {
            DataTable dt = new DataTable();
            try
            {
                MySqlConnection conn = mConexion.getConexion();
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);

                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al obtener datos:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


            return dt;
        }
    }
}
