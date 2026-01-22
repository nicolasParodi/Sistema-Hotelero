using iTextSharp.text;
using iTextSharp.text.pdf;
using MySql.Data.MySqlClient;
using Sistema_Hotelero.Formularios;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace Sistema_Hotelero
{
    public partial class FormNuevaReserva: Form
    {
        private Conexion mConexion;

        public FormNuevaReserva()
        {
            InitializeComponent();
        }

        private void FormNuevaReserva_Load(object sender, EventArgs e)
        {
            mConexion = new Conexion();
            CargarHabitaciones();
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

                cmbCantHuespedes.SelectedIndex = 0;
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

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            //validaciones basicas
            if (txtNombre.Text == "" || txtApellido.Text == "" || txtDocumento.Text == "" || txtDireccion.Text == "" || txtTelefono.Text == "" ||
                cmbHabitacion.SelectedValue == null)
            {
                MessageBox.Show("Complete todos los campos obligatorios.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
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

                    //Insertar reserva
                    MySqlCommand cmdReserva = new MySqlCommand(@"INSERT INTO reservas(idHabitacion, huespedes, fechaIngreso, fechaEgreso,
                                                               vehiculo, patente) VALUES (@hab, @hues, @fi, @fe, @veh, @pat); SELECT
                                                               LAST_INSERT_ID();", conn);
                    cmdReserva.Parameters.AddWithValue("@hab", cmbHabitacion.SelectedValue.ToString());
                    cmdReserva.Parameters.AddWithValue("@hues", cmbCantHuespedes.Text);
                    cmdReserva.Parameters.AddWithValue("@fi", dtpIngreso.Value.Date);
                    cmdReserva.Parameters.AddWithValue("@fe", dtpEgreso.Value.Date);
                    cmdReserva.Parameters.AddWithValue("@veh", txtVehiculo.Text);
                    cmdReserva.Parameters.AddWithValue("@pat", txtPatente.Text);
                    int idReserva = Convert.ToInt32(cmdReserva.ExecuteScalar());

                    //Insertar relacion detalle_reserva
                    MySqlCommand cmdDetalle = new MySqlCommand(@"INSERT INTO detalle_reserva (idHuesped, idReserva, esPrincipal) VALUES
                                                              (@h, @r, @ep);", conn);
                    cmdDetalle.Parameters.AddWithValue("@h", idHuesped);
                    cmdDetalle.Parameters.AddWithValue("@r", idReserva);
                    cmdDetalle.Parameters.AddWithValue("@ep", 1);
                    cmdDetalle.ExecuteNonQuery();

                    //Verificar si hay mas huespedes
                    int cant = Convert.ToInt32(cmbCantHuespedes.SelectedItem);

                    if (cant > 1)
                    {
                        for (int i = 2; i <= cant; i++)
                        {
                            FormHuespedAdicional frm = new FormHuespedAdicional(idReserva);
                            frm.Text = $"Huésped adicional #{i}";
                            frm.ShowDialog();
                        }
                    }

                    DialogResult resp = MessageBox.Show("Reserva registrada con éxito.\n ¿Desea Imprimir el detalle de la reserva?",
                                                        "Éxito", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (resp == DialogResult.Yes)
                    {
                        GenerarPDF(idReserva);
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al registrar reserva: " + ex.Message);
                throw;
            }
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
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Habitación:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD,10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase(habitacionNombre, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Fecha ingreso:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase(ingreso.ToString("dd/MM/yyyy"), FontFactory.GetFont(
                                                                FontFactory.HELVETICA, 10))){ Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Fecha egreso:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase(egreso.ToString("dd/MM/yyyy"), FontFactory.GetFont(
                                                                FontFactory.HELVETICA, 10))){ Border = 0 });
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Vehículo:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase(vehiculo, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase("Patente:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10)))
                                                                { Border = 0});
                    tablaReserva.AddCell(new PdfPCell(new Phrase(patente, FontFactory.GetFont(FontFactory.HELVETICA, 10)))
                                                                { Border = 0});
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
    }
}
