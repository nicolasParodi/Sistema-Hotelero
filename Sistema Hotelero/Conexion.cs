using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sistema_Hotelero
{
    class Conexion
    {
        private string server = "localhost";
        private string database = "sistema de hoteles";
        private string user = "root";
        private string password = "";
        private string cadenaConexion;
        private MySqlConnection conexion;

        public Conexion()
        {
            cadenaConexion = "DataBase=" + database + "; DataSource=" + server + "; User Id=" + user + "; Password=" + password;
        }

        public MySqlConnection getConexion()
        {
            conexion = new MySqlConnection(cadenaConexion);
            conexion.Open();

            return conexion;
        }

        public void EjecutarConsulta(string query)
        {
            using (MySqlConnection conn = getConexion())
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
