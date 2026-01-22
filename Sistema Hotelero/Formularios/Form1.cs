using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using Sistema_Hotelero.Formularios;
using System.IO;
using PdfText = iTextSharp.text;
using PdfWriter = iTextSharp.text.pdf;
using System.Xml.Linq;
using System.Drawing;
using System.Diagnostics;
using Org.BouncyCastle.Asn1.Cmp;
using System.Windows.Forms.DataVisualization.Charting;

namespace Sistema_Hotelero
{
    public partial class Form1: Form
    {
        //Conexion a la base de datos
        private Conexion mConexion;
        private int MesSeleccionado;
        private int anioSeleccionado;
        private int? idHabitacion = null;
        private int? idProducto = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mConexion = new Conexion();

            // Oculta las pestañas visualmente
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;

            //LLenar combos de año y mes
            for (int año = DateTime.Now.Year - 3; año <= DateTime.Now.Year + 3; año++)
            {
                cmbAnio.Items.Add(año);
            }

            cmbMes.Items.AddRange(new object[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio",
                                                 "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"});

            cmbAnio.SelectedItem = DateTime.Now.Year;
            cmbMes.SelectedIndex = DateTime.Now.Month - 1;

            MesSeleccionado = DateTime.Now.Month;
            anioSeleccionado = DateTime.Now.Year;
            CargarCalendario();
        }

        private DataTable ObtenerDatos(string query)
        {
            DataTable dt = new DataTable();
            try
            {
                MySqlConnection conn = mConexion.getConexion();
                if (conn.State !=  ConnectionState.Open)
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

        //Calendario Reserva

        private void btnActualizar_Click(object sender, EventArgs e)
        {
            CargarCalendario();
        }

        private void CargarCalendario()
        {
            if (cmbMes.SelectedIndex < 0 || cmbAnio.SelectedIndex < 0) return;

            int mes = cmbMes.SelectedIndex + 1;
            int año = (int)cmbAnio.SelectedItem;
            int diasEnMes = DateTime.DaysInMonth(año, mes);

            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            // Crear columnas
            dataGridView1.Columns.Add("Habitación", "Habitación");
            for (int d = 1; d <= diasEnMes; d++)
            {
                dataGridView1.Columns.Add("D" + d, d.ToString());
            }

            bool hayReservas = false;

            // Obtener habitaciones
            DataTable habitaciones = ObtenerDatos("SELECT ID, habitacion FROM habitaciones");

            foreach (DataRow hab in habitaciones.Rows)
            {
                int rowIndex = dataGridView1.Rows.Add();
                dataGridView1.Rows[rowIndex].Cells["Habitación"].Value = hab["habitacion"].ToString();

                // 🔹 CORRECCIÓN 1: usar hbt.ID (clave) en lugar de hbt.habitacion
                string query = $@"SELECT r.ID, r.fechaIngreso, r.fechaEgreso, hbt.habitacion AS nombreHabitacion, CONCAT(h.nombre, ' ', h.apellido) AS huespedNombre, h.documento,
                                  h.telefono, r.vehiculo, r.patente, (SELECT COUNT(*) FROM detalle_reserva dr WHERE dr.idReserva = r.ID) AS cantidadHuespedes FROM reservas r
                                  INNER JOIN detalle_reserva dr ON dr.idReserva = r.ID INNER JOIN huesped h ON h.ID = dr.idHuesped INNER JOIN habitaciones hbt ON
                                  r.idHabitacion = hbt.ID WHERE dr.esPrincipal = 1 AND r.idHabitacion = {hab["ID"]} AND MONTH(r.fechaIngreso) = {mes} AND
                                  YEAR(r.fechaIngreso) = {año};";

                DataTable reservas = ObtenerDatos(query);

                foreach (DataRow res in reservas.Rows)
                {
                    DateTime ingreso = Convert.ToDateTime(res["fechaIngreso"]);
                    DateTime egreso = Convert.ToDateTime(res["fechaEgreso"]);

                    hayReservas = true;

                    // 🔹 CORRECCIÓN 2: incluir el día de salida y verificar que no se pase del mes
                    for (int d = ingreso.Day; d < egreso.Day && d <= diasEnMes; d++)
                    {
                        string colName = "D" + d;
                        if (!dataGridView1.Columns.Contains(colName)) continue;

                        // 🔹 CORRECCIÓN 3: formato legible y con saltos de línea
                        string info = $"{res["huespedNombre"]}\nDNI: {res["documento"]}\nVehículo: {res["vehiculo"]}\n" +
                                      $"Patente: {res["patente"]}\n{res["cantidadHuespedes"]} huéspedes";

                        dataGridView1.Rows[rowIndex].Cells[colName].Value = info;
                        dataGridView1.Rows[rowIndex].Cells[colName].Style.BackColor = Color.LightSkyBlue;
                        dataGridView1.Rows[rowIndex].Cells[colName].Style.ForeColor = Color.Black;
                        dataGridView1.Rows[rowIndex].Cells[colName].Style.WrapMode = DataGridViewTriState.True;
                    }
                }
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            // --- Estilos visuales ---
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            // --- Ajuste automático del tamaño ---
            if (hayReservas)
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells; // 🔹 Ajusta TODAS las columnas al contenido
            }
            else
            {
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // 🔹 Ajusta TODAS las columnas al contenido
            }
                dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;       // 🔹 Ajusta la altura de las filas
            dataGridView1.ScrollBars = ScrollBars.Both;

            // --- Fijar (freezar) la primera columna ---
            dataGridView1.Columns["Habitación"].Frozen = true;

            // --- Estilos del encabezado ---
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.LightSteelBlue;

            // --- Refrescar ---
            dataGridView1.Refresh();
        }

        private void btnNuevaReserva_Click(object sender, EventArgs e)
        {
            FormNuevaReserva frm = new FormNuevaReserva();
            frm.ShowDialog();
            CargarCalendario();//actualizar al cerrar
        }

        private void rESERVASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
            CargarCalendario();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ignorar encabezados
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            // Obtener la habitación y el día clickeado
            string habitacion = dataGridView1.Rows[e.RowIndex].Cells["Habitación"].Value.ToString();
            string diaCol = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            // Validar que haya una reserva cargada en esa celda
            if (dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == null)
                return;

            // Buscar la reserva asociada
            string query = $@"SELECT r.ID FROM reservas r INNER JOIN habitaciones h ON r.idHabitacion = h.ID WHERE h.habitacion = '{habitacion}' AND 
                           DAY(r.fechaIngreso) <= {diaCol} AND DAY(r.fechaEgreso) > {diaCol};";

            DataTable result = ObtenerDatos(query);
            if (result.Rows.Count > 0)
            {
                int idReserva = Convert.ToInt32(result.Rows[0]["ID"]);

                // Abrir formulario de edición
                FormEditarReserva formEditar = new FormEditarReserva (idReserva);
                formEditar.ShowDialog();

                // Luego de cerrar el formulario, recargar el calendario
                CargarCalendario();
            }
        }

        //Habitaciones

        private void hABITACIONESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            cargarHabitaciones();
            btnEditar.Enabled = false;
        }

        private void cargarHabitaciones()
        {
            dataGridView2.Rows.Clear();
            string query = "SELECT h.* FROM habitaciones h";
            DataTable habitaciones = ObtenerDatos(query);

            foreach (DataRow hab in habitaciones.Rows)
            {
                int rowIndex = dataGridView2.Rows.Add();
                dataGridView2.Rows[rowIndex].Cells["idHab"].Value = hab["ID"].ToString();
                dataGridView2.Rows[rowIndex].Cells["Habitacion"].Value = hab["habitacion"].ToString();
                dataGridView2.Rows[rowIndex].Cells["Camas"].Value = hab["camas"].ToString();
                dataGridView2.Rows[rowIndex].Cells["Estado"].Value = hab["estado"].ToString();
            }

            dataGridView2.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView2.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dataGridView2.RowHeadersVisible = false;
            dataGridView2.AllowUserToResizeRows = false;
            dataGridView2.AllowUserToResizeColumns = false;
            dataGridView2.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        }

        private void btnCargarHabitacion_Click(object sender, EventArgs e)
        {
            string nombre = txtHabitacion.Text.Trim();
            int camas = Convert.ToInt32(txtCamas.Text.Trim());
            string estado = cmbEstado.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(estado))
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = mConexion.getConexion())
            {
                string query = "INSERT INTO habitaciones (habitacion, camas, estado) VALUES (@habitacion, @camas, @estado)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@habitacion", nombre);
                cmd.Parameters.AddWithValue("@camas", camas);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Habitación cargada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            limpiarCamposHabitaciones();
            cargarHabitaciones();
        }

        private void limpiarCamposHabitaciones()
        {
            txtHabitacion.Clear();
            txtCamas.Clear();
            cmbEstado.SelectedIndex = -1;
        }

        private void dgvHabitaciones_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                idHabitacion = Convert.ToInt32(dataGridView2.Rows[e.RowIndex].Cells["idHab"].Value);
                txtHabitacion.Text = dataGridView2.Rows[e.RowIndex].Cells["Habitacion"].Value.ToString();
                txtCamas.Text = dataGridView2.Rows[e.RowIndex].Cells["Camas"].Value.ToString();
                cmbEstado.SelectedItem = dataGridView2.Rows[e.RowIndex].Cells["Estado"].Value.ToString();

                btnCargarHabitacion.Enabled = false;
                btnEditar.Enabled = true;
            }
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            string nombre = txtHabitacion.Text.Trim();
            int camas = Convert.ToInt32(txtCamas.Text.Trim());
            string estado = cmbEstado.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(estado))
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = mConexion.getConexion())
            {
                string query = "UPDATE habitaciones SET habitacion=@habitacion, camas=@camas, estado=@estado WHERE ID=@id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@habitacion", nombre);
                cmd.Parameters.AddWithValue("@camas", camas);
                cmd.Parameters.AddWithValue("@estado", estado);
                cmd.Parameters.AddWithValue("@id", idHabitacion);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Habitación actualizada correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            limpiarCamposHabitaciones();
            cargarHabitaciones();
            btnCargarHabitacion.Enabled = true;
            btnEditar.Enabled = false;
            idHabitacion = null;
        }

        //Almacen

        private void aLMACENToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 2;
            cargarAlmacen();
            btnAgregar.Enabled = false;
            btnUsar.Enabled = false;
            btnEliminar.Enabled = false;
        }

        private void dgvAlmacen_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex >= 0)
            {
                idProducto = Convert.ToInt32(dataGridView3.Rows[e.RowIndex].Cells["idProd"].Value);
                txtProducto.Text = dataGridView3.Rows[e.RowIndex].Cells["Articulo"].Value.ToString();
                txtCantidad.Text = dataGridView3.Rows[e.RowIndex].Cells["Cantidad"].Value.ToString();

                btnNuevoProducto.Enabled = true;
                btnUsar.Enabled = false;
                btnEliminar.Enabled = false;
                btnAgregar.Enabled = false;
                idHabitacion = null;
            }
        }

        private void cargarAlmacen()
        {
            dataGridView3.Rows.Clear();
            string query = "SELECT a.* FROM almacen a";
            DataTable almacen = ObtenerDatos(query);
            foreach (DataRow alm in almacen.Rows)
            {
                int rowIndex = dataGridView3.Rows.Add();
                dataGridView3.Rows[rowIndex].Cells["idProd"].Value = alm["ID"].ToString();
                dataGridView3.Rows[rowIndex].Cells["Articulo"].Value = alm["articulo"].ToString();
                dataGridView3.Rows[rowIndex].Cells["Cantidad"].Value = alm["stock"].ToString();
            }

            dataGridView3.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView3.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dataGridView3.RowHeadersVisible = false;
            dataGridView3.AllowUserToResizeRows = false;
            dataGridView3.AllowUserToResizeColumns = false;
            dataGridView3.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        }

        private void btnNuevoProducto_Click(object sender, EventArgs e)
        {
            string producto = txtProducto.Text.Trim();
            int cantidad = Convert.ToInt32(txtCantidad.Text.Trim());

            if (string.IsNullOrEmpty(producto))
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using(MySqlConnection conn = mConexion.getConexion())
            {
                string query = "INSERT INTO almacen (articulo, stock) VALUES (@articulo, @stock";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@articulo", producto);
                cmd.Parameters.AddWithValue("@stock", cantidad);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Producto cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            limpiarCamposAlmacen();
            cargarAlmacen();
        }

        private void limpiarCamposAlmacen()
        {
            txtProducto.Clear();
            txtCantidad.Clear();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            using (MySqlConnection conn = mConexion.getConexion())
            {
                string deleteQuery = "DELETE FROM almacen WHERE ID = @id";
                MySqlCommand cmdDel = new MySqlCommand(deleteQuery, conn);
                cmdDel.Parameters.AddWithValue("@id", idProducto);
                cmdDel.ExecuteNonQuery();
            }

            limpiarCamposHabitaciones();
            cargarHabitaciones();
            btnNuevoProducto.Enabled = true;
            btnUsar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAgregar.Enabled = false;
            idHabitacion = null;
        }

        private void btnUsar_Click(object sender, EventArgs e)
        {
            string producto = txtProducto.Text.Trim();
            int cantidad = Convert.ToInt32(txtCantidad.Text.Trim());

            if (string.IsNullOrEmpty(producto))
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using(MySqlConnection conn = mConexion.getConexion())
            {
                string querySelect = "SELECT stock FROM almacen WHERE ID=@id";
                MySqlCommand cmdSelect = new MySqlCommand(querySelect, conn);
                cmdSelect.Parameters.AddWithValue("@id", idProducto);
                object result = cmdSelect.ExecuteScalar();
                int stockActual = Convert.ToInt32(result);

                if (cantidad>stockActual)
                {
                    MessageBox.Show("No hay suficiente stock disponible.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                

                string queryUpdate = "UPDATE almacen SET articulo = @articulo, stock = stock - @stock WHERE ID=@id";
                MySqlCommand cmdUpdate = new MySqlCommand(queryUpdate, conn);
                cmdUpdate.Parameters.AddWithValue("@articulo", producto);
                cmdUpdate.Parameters.AddWithValue("@stock", cantidad);
                cmdUpdate.Parameters.AddWithValue("@id", idProducto);
                cmdUpdate.ExecuteNonQuery();
            }

            MessageBox.Show("Productos descontados correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            limpiarCamposHabitaciones();
            cargarHabitaciones();
            btnNuevoProducto.Enabled = true;
            btnUsar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAgregar.Enabled = false;
            idHabitacion = null;
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            string producto = txtProducto.Text.Trim();
            int cantidad = Convert.ToInt32(txtCantidad.Text.Trim());

            if (string.IsNullOrEmpty(producto))
            {
                MessageBox.Show("Complete todos los campos.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection conn = mConexion.getConexion())
            {
                string query = "UPDATE almacen SET articulo = @articulo, stock = stock + @stock WHERE ID=@id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@articulo", producto);
                cmd.Parameters.AddWithValue("@stock", cantidad);
                cmd.Parameters.AddWithValue("@id", idProducto);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Productos agregados correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            limpiarCamposHabitaciones();
            cargarHabitaciones();
            btnNuevoProducto.Enabled = true;
            btnUsar.Enabled = false;
            btnEliminar.Enabled = false;
            btnAgregar.Enabled = false;
            idHabitacion = null;
        }

        //Empleados

        private void pERSONALToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 3;
            cargarPersonal();
        }

        private void dgvEmpleados_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                int idEmpleado = Convert.ToInt32(dataGridView4.Rows[e.RowIndex].Cells["ID"].Value);

                // Abrimos el formulario de edición y le pasamos el ID
                FormEmpleado formEdit = new FormEmpleado(idEmpleado);
                formEdit.ShowDialog();

                // Al cerrar, recargamos la tabla
                cargarPersonal();
            }
        }

        private void btnContratar_Click(object sender, EventArgs e)
        {
            FormEmpleado frm = new FormEmpleado();
            frm.ShowDialog();
            cargarPersonal();
        }

        private void cargarPersonal()
        {
            dataGridView4.Rows.Clear();
            string query = "SELECT p.* FROM personal p";
            DataTable personal = ObtenerDatos(query);
            dataGridView4.Rows.Clear();
            dataGridView4.Refresh();
            foreach (DataRow per in personal.Rows)
            {
                int rowIndex = dataGridView4.Rows.Add();
                dataGridView4.Rows[rowIndex].Cells["ID"].Value = per["ID"].ToString();
                dataGridView4.Rows[rowIndex].Cells["Nombre"].Value = per["nombre"].ToString();
                dataGridView4.Rows[rowIndex].Cells["Apellido"].Value = per["apellido"].ToString();
                dataGridView4.Rows[rowIndex].Cells["Documento"].Value = per["documento"].ToString();
                dataGridView4.Rows[rowIndex].Cells["Telefono"].Value = per["telefono"].ToString();
                dataGridView4.Rows[rowIndex].Cells["Sueldo"].Value = per["sueldo"].ToString();
                dataGridView4.Rows[rowIndex].Cells["FechaDePago"].Value = per["fechaPago"].ToString();
            }

            dataGridView4.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView4.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dataGridView4.RowHeadersVisible = false;
            dataGridView4.AllowUserToResizeRows = false;
            dataGridView4.AllowUserToResizeColumns = false;
            dataGridView4.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        }

        private void btnPagar_Click(object sender, EventArgs e)
        {
            if (dataGridView4.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un empleado.");
                return;
            }

            int idEmpleado = Convert.ToInt32(dataGridView4.CurrentRow.Cells["ID"].Value);
            DateTime nuevaFecha = DateTime.Now.AddMonths(1);

            string query = "UPDATE personal SET fechaPago = @fechaPago WHERE ID = @id";
            using (MySqlConnection conn = mConexion.getConexion())
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@fechaPago", nuevaFecha);
                cmd.Parameters.AddWithValue("@id", idEmpleado);
                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("Pago actualizado correctamente.", "Éxito");
            cargarPersonal();
        }

        private void btnDespedir_Click(object sender, EventArgs e)
        {
            if (dataGridView4.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleccione un empleado.");
                return;
            }

            int idEmpleado = Convert.ToInt32(dataGridView4.CurrentRow.Cells["ID"].Value);

            string query = "SELECT * FROM personal WHERE ID = @id";
            using (MySqlConnection conn = mConexion.getConexion())
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", idEmpleado);
                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string nombre = reader["nombre"].ToString();
                    string apellido = reader["apellido"].ToString();
                    string documento = reader["documento"].ToString();
                    decimal sueldo = Convert.ToDecimal(reader["sueldo"]);
                    DateTime fechaAlta = Convert.ToDateTime(reader["fechaAlta"]);
                    DateTime hoy = DateTime.Now;

                    reader.Close();

                    // --- Calcular montos ---
                    int diasTrabajadosMes = hoy.Day;
                    int diasDelMes = DateTime.DaysInMonth(hoy.Year, hoy.Month);
                    decimal salarioProporcional = sueldo * diasTrabajadosMes / diasDelMes;

                    int mesesTrabajadosSemestre = (hoy.Month - 1) % 6 + 1;
                    decimal sacProporcional = (sueldo / 12) * mesesTrabajadosSemestre;

                    double diasTrabajadosAño = (hoy - fechaAlta).TotalDays;
                    double vacaciones = 14 * diasTrabajadosAño / 365;
                    decimal vacacionesProporcionales = (sueldo / 25) * (decimal)vacaciones;

                    decimal total = salarioProporcional + sacProporcional + vacacionesProporcionales;

                    // --- Confirmar antes de eliminar ---
                    string detalle = $"Liquidación de {nombre} {apellido}\n\n" +
                                     $"Salario proporcional: ${salarioProporcional:F2}\n" +
                                     $"SAC proporcional: ${sacProporcional:F2}\n" +
                                     $"Vacaciones proporcionales: ${vacacionesProporcionales:F2}\n" +
                                     $"---------------------------------\n" +
                                     $"Total a pagar: ${total:F2}\n\n¿Desea generar el documento e iniciar la baja?";

                    DialogResult result = MessageBox.Show(detalle, "Confirmar liquidación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No) return;

                    // --- Crear PDF ---
                    string folderPath = Path.Combine(Application.StartupPath, "Liquidaciones");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string filePath = Path.Combine(folderPath, $"Liquidacion_{apellido}_{nombre}_{DateTime.Now:yyyyMMdd}.pdf");

                    GenerarContratoLiquidacionPDF(filePath, nombre, apellido, documento, sueldo, fechaAlta, hoy,
                                                  salarioProporcional, sacProporcional, vacacionesProporcionales, total);

                    // --- Eliminar de la base ---
                    string deleteQuery = "DELETE FROM personal WHERE ID = @id";
                    MySqlCommand cmdDel = new MySqlCommand(deleteQuery, conn);
                    cmdDel.Parameters.AddWithValue("@id", idEmpleado);
                    cmdDel.ExecuteNonQuery();

                    MessageBox.Show($"Liquidación generada y empleado eliminado.\nPDF: {filePath}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    cargarPersonal();
                }
            }
        }

        private void GenerarContratoLiquidacionPDF(string filePath, string nombre, string apellido, string documento, decimal sueldo,
                                           DateTime fechaAlta, DateTime fechaBaja, decimal salarioProp, decimal sacProp,
                                           decimal vacacionesProp, decimal total)
        {
            // Crear carpeta si no existe
            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Crear documento PDF
            PdfText.Document doc = new PdfText.Document(PdfText.PageSize.A4, 50, 50, 50, 50);
            PdfWriter.PdfWriter.GetInstance(doc, new FileStream(filePath, FileMode.Create));
            doc.Open();

            // Fuentes
            PdfText.Font tituloFont = PdfText.FontFactory.GetFont("Helvetica", 18, PdfText.Font.BOLD);
            PdfText.Font textoFont = PdfText.FontFactory.GetFont("Helvetica", 12, PdfText.Font.NORMAL);
            PdfText.Font boldFont = PdfText.FontFactory.GetFont("Helvetica", 12, PdfText.Font.BOLD);

            // 🔹 Encabezado
            PdfText.Paragraph titulo = new PdfText.Paragraph("CONTRATO DE LIQUIDACIÓN FINAL", tituloFont)
            {
                Alignment = PdfText.Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            doc.Add(titulo);

            // 🔹 Datos del empleado
            PdfText.Paragraph datosEmpleado = new PdfText.Paragraph(
                $"Empleado: {nombre} {apellido}\n" +
                $"Documento: {documento}\n" +
                $"Fecha de Ingreso: {fechaAlta:dd/MM/yyyy}\n" +
                $"Fecha de Baja: {fechaBaja:dd/MM/yyyy}\n" +
                $"Sueldo base mensual: ${sueldo:N2}\n",
                textoFont
            )
            { SpacingAfter = 20 };
            doc.Add(datosEmpleado);

            // 🔹 Cuerpo del contrato
            string cuerpoTexto =
                "Por medio del presente documento, se deja constancia de la liquidación final correspondiente al empleado mencionado. " +
                "El mismo recibe en concepto de liquidación por renuncia los siguientes ítems: salario proporcional, aguinaldo (SAC) proporcional, " +
                "y vacaciones no gozadas proporcionales, conforme a la legislación laboral vigente.\n\n" +
                "El empleador declara haber abonado todas las sumas adeudadas hasta la fecha de baja laboral.\n\n";

            doc.Add(new PdfText.Paragraph(cuerpoTexto, textoFont) { SpacingAfter = 20 });

            // 🔹 Tabla con el detalle de los importes
            iTextSharp.text.pdf.PdfPTable tabla = new iTextSharp.text.pdf.PdfPTable(2);
            tabla.WidthPercentage = 80;
            tabla.HorizontalAlignment = PdfText.Element.ALIGN_CENTER;

            tabla.AddCell(new PdfText.Phrase("Concepto", boldFont));
            tabla.AddCell(new PdfText.Phrase("Monto ($)", boldFont));

            tabla.AddCell(new PdfText.Phrase("Salario proporcional", textoFont));
            tabla.AddCell(new PdfText.Phrase(salarioProp.ToString("N2"), textoFont));

            tabla.AddCell(new PdfText.Phrase("SAC proporcional", textoFont));
            tabla.AddCell(new PdfText.Phrase(sacProp.ToString("N2"), textoFont));

            tabla.AddCell(new PdfText.Phrase("Vacaciones proporcionales", textoFont));
            tabla.AddCell(new PdfText.Phrase(vacacionesProp.ToString("N2"), textoFont));

            tabla.AddCell(new PdfText.Phrase("TOTAL LIQUIDACIÓN", boldFont));
            tabla.AddCell(new PdfText.Phrase(total.ToString("N2"), boldFont));

            doc.Add(tabla);
            doc.Add(new PdfText.Paragraph("\n\n"));

            // 🔹 Firma
            PdfText.Paragraph firma = new PdfText.Paragraph(
                "\n\n_____________________________\nFirma del Empleado\n",
                textoFont
            )
            { Alignment = PdfText.Element.ALIGN_CENTER };
            doc.Add(firma);

            doc.Close();

            // Abrir automáticamente el PDF generado
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }

            MessageBox.Show($"PDF de liquidación generado exitosamente:\n{filePath}",
                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        //Finanzas

        private void fINANZASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 4;
            for (int año = DateTime.Now.Year - 3; año <= DateTime.Now.Year + 3; año++)
            {
                comboBox2.Items.Add(año);
            }

            comboBox1.Items.AddRange(new object[] { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio",
                                                 "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"});

            comboBox2.SelectedItem = DateTime.Now.Year;
            comboBox1.SelectedIndex = DateTime.Now.Month - 1;

            MesSeleccionado = DateTime.Now.Month;
            anioSeleccionado = DateTime.Now.Year;
            CargarGraficoFinanzas();
        }

        private void cargarTableFinanzas(int mes, int anio)
        {
            try
            {
                using (MySqlConnection conn = mConexion.getConexion())
                {
                    string query = @" SELECT id, tipo,CASE  WHEN tipo = 'INGRESO' THEN monto WHEN tipo = 'EGRESO' THEN -monto ELSE monto END AS monto, fecha, concepto FROM 
                                   finanzas WHERE MONTH(fecha) = @mes AND YEAR(fecha) = @anio ORDER BY fecha DESC;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@mes", mes);
                    cmd.Parameters.AddWithValue("@anio", anio);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dataGridView5.Rows.Clear();

                    foreach (DataRow row in dt.Rows)
                    {
                        int index = dataGridView5.Rows.Add();

                        dataGridView5.Rows[index].Cells["idFinanzas"].Value = row["id"];
                        dataGridView5.Rows[index].Cells["tipo"].Value = row["tipo"];
                        dataGridView5.Rows[index].Cells["monto"].Value = Convert.ToDecimal(row["monto"]);
                        dataGridView5.Rows[index].Cells["fecha"].Value = Convert.ToDateTime(row["fecha"]).ToString("dd/MM/yyyy");
                        dataGridView5.Rows[index].Cells["concepto"].Value = row["concepto"];
                    }
                }

                // Opcional: Colorear ingresos y egresos
                foreach (DataGridViewRow row in dataGridView5.Rows)
                {
                    if (row.Cells["tipo"].Value?.ToString() == "INGRESO")
                    {
                        row.DefaultCellStyle.ForeColor = Color.Green;
                    }
                    else if (row.Cells["tipo"].Value?.ToString() == "EGRESO")
                    {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos de finanzas:\n" + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGraficoFinanzas()
        {

            int mes, anio;

            using (MySqlConnection conn = mConexion.getConexion())
            {
                if (comboBox1.SelectedIndex == -1 || comboBox2.SelectedIndex == -1)
                {
                    MessageBox.Show("Seleccione un mes y un año para mostrar los datos.",
                                    "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Detectar si los combo son numéricos o con texto
                if (int.TryParse(comboBox1.SelectedItem.ToString(), out mes))
                    mes = Convert.ToInt32(comboBox1.SelectedItem);
                else
                    mes = comboBox1.SelectedIndex + 1; // si los meses están como nombres ("Enero", "Febrero"…)

                anio = Convert.ToInt32(comboBox2.SelectedItem);

                // Traemos los ingresos y egresos del mes actual
                string query = @" SELECT tipo, SUM(monto) AS total FROM finanzas WHERE MONTH(fecha) = @mes AND YEAR(fecha) = @año GROUP BY tipo;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@mes", mes);
                cmd.Parameters.AddWithValue("@año", anio);
                MySqlDataReader reader = cmd.ExecuteReader();

                decimal ingresos = 0;
                decimal egresos = 0;

                while (reader.Read())
                {
                    string tipo = reader["tipo"].ToString();
                    decimal total = Convert.ToDecimal(reader["total"]);

                    if (tipo.Equals("INGRESO", StringComparison.OrdinalIgnoreCase))
                        ingresos = total;
                    else if (tipo.Equals("EGRESO", StringComparison.OrdinalIgnoreCase))
                        egresos = total;
                }

                reader.Close();

                //Modificar label
                lblIngresos.Text = ingresos.ToString();
                lblEgresos.Text = egresos.ToString();
                decimal balance = ingresos - egresos;
                lblBalance.Text = balance.ToString();

                // Configuración del gráfico
                chartFinanzas.Series.Clear();
                chartFinanzas.Titles.Clear();

                chartFinanzas.Titles.Add("Finanzas del Mes");

                Series serie = new Series("Finanzas");
                serie.ChartType = SeriesChartType.Pie;
                serie.IsValueShownAsLabel = true;
                serie.LabelFormat = "C"; // formato moneda

                serie.Points.AddXY("Ingresos", ingresos);
                serie.Points.AddXY("Egresos", egresos);

                chartFinanzas.Series.Add(serie);

                // Opcional: mejorar estética
                chartFinanzas.ChartAreas[0].Area3DStyle.Enable3D = true;
                chartFinanzas.Legends[0].Enabled = true;
                chartFinanzas.Legends[0].Docking = Docking.Bottom;
            }

            cargarTableFinanzas(mes, anio);
        }

        private void btnActualizarFinanzas_Click(object sender, EventArgs e)
        {
            CargarGraficoFinanzas();
        }

        private void btnIngreso_Click(object sender, EventArgs e)
        {
            FormIngresoEgreso form = new FormIngresoEgreso("INGRESO");
            form.ShowDialog();
            CargarGraficoFinanzas();
        }

        private void btnEgreso_Click(object sender, EventArgs e)
        {

            FormIngresoEgreso form = new FormIngresoEgreso("EGRESO");
            form.ShowDialog();
            CargarGraficoFinanzas();
        }
    }
}
