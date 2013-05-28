using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using System.Drawing;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using NpgsqlTypes;

namespace MJr_SQLServer2PostgreSQL
{
    public class Program
    {
        private string gServidorSQLServer = "", gServidorPostgreSQL = "";
        private string gTabelaDestino = "", gTabelaOrigem = "";
        private string strConnSQL = "";
        private string strConnPgSql = "";
        private bool gApagar = false;
        private int gTipoRepresentacao = 0;
  
        static void Main(string[] args)
        {
            Program prg = new Program();

            try
            {
                int i = 0;
                prg.Log("MJr - SQLServer to PostgreSQL - Entrando no Sistema");

                //prg.ServidorSQLServer = "(local)";
                //prg.ServidorPostgreSQL = "localhost";
                //prg.TabelaOrigem = "texts11";
                //prg.TabelaDestino = "texts1";
                //prg.Apagar = true;
                //prg.TipoRepresentacao = 128;

                foreach (string arg in args)
                {
                    if (i == 0)
                        prg.ServidorSQLServer = arg;
                    if (i == 1)
                        prg.ServidorPostgreSQL = arg;
                    if (i == 2)
                        prg.TabelaOrigem = arg;
                    if (i == 3)
                        prg.TabelaDestino = arg;
                    if (i == 4)
                    {
                        if (arg == "1")
                            prg.Apagar = true;
                        else
                            prg.Apagar = false;
                    }
                    if (i == 5)
                    {
                        prg.TipoRepresentacao = Convert.ToInt32(arg);
                    } 
                    i++;
                }

                prg.Log("Servidor SQLServer - " + prg.ServidorSQLServer);
                prg.Log("Servidor PostgreSQL - " + prg.ServidorPostgreSQL);
                prg.Log("Tabela Origem - " + prg.TabelaOrigem);
                prg.Log("Tabela Destino - " + prg.TabelaDestino);
                prg.Log("Apagar - " + prg.Apagar.ToString());
                prg.Log("Representacao - " + prg.TipoRepresentacao.ToString());
                
                if (prg.TabelaDestino.Length > 0)
                    if (prg.TabelaOrigem.Length > 0)
                        prg.CopiarTabela(prg);
                    
                prg.Log("MJr - SQLServer to PostgreSQL - Saindo do Sistema...");
                Application.Exit();
            }
            catch (Exception ex)
            {
                prg.Log(ex.ToString());
            }
        }

        private void Log(string texto)
        {
            try
            {
                DirectoryInfo diretorio = new DirectoryInfo("c:\\Temp");
                diretorio.Create();
                FileStream arquivo = new FileStream(diretorio + "\\MJr-SQLServer2PostgreSQL.txt", FileMode.Append, FileAccess.Write, FileShare.None);
                StreamWriter sw = new StreamWriter(arquivo);
                sw.WriteLine(texto);
                sw.Close();
                arquivo.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
        }

        public string TabelaOrigem
        {
            set
            {
                gTabelaOrigem = value;
            }
            get
            {
                return gTabelaOrigem;
            }
        }

        public string ServidorSQLServer
        {
            set
            {
                gServidorSQLServer = value;
            }
            get
            {
                return gServidorSQLServer;
            }
        }

        public string TabelaDestino
        {
            set
            {
                gTabelaDestino = value;
            }
            get
            {
                return gTabelaDestino;
            }
        }

        public string ServidorPostgreSQL
        {
            set
            {
                gServidorPostgreSQL = value;
            }
            get
            {
                return gServidorPostgreSQL;
            }

        }

        private void CopiarTabelaPolygono(Program prg)
        {
            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                prg.Log("Copiando Tabela");

                strConnSQL = "Server=" + ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                strConnPgSql = "server=" + ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
                string consulta = "";
                Int32 npt = 0;
                Int32 qtd = 0;

                prg.Log(strConnSQL);
                prg.Log(strConnPgSql);

                NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql);
                connPgSql.Open();

                if (Apagar)
                {
                    tw.Progress("Apagando tabela...", prg.TabelaDestino);
                    prg.Log("Apagando Registros");
                    prg.Log("Tabela - " + prg.TabelaDestino);

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        try
                        {
                            cmd.Connection = connPgSql;
                            cmd.CommandText = "delete from " + prg.TabelaDestino;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            prg.Log(ex.ToString());
                        }
                    }

                    prg.Log("Tabela - " + prg.TabelaDestino + " - Registros Apagados");
                }

                prg.Log("select * from " + prg.TabelaDestino + " where 1=2");
                
                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();
                    consulta = "select count(*) as qtd from " + prg.TabelaOrigem;
                    prg.Log(consulta);
                    SqlCommand cmdCount = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = cmdCount.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            qtd = Convert.ToInt32(reader["qtd"]);
                            prg.Log(qtd + " registros...");
                        }
                    }

                    consulta = "select * from " + prg.TabelaOrigem;
                    SqlCommand command = new SqlCommand(consulta, connSql);

                    using(SqlDataReader reader = command.ExecuteReader())
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand())
                        {
                            Double x, y;
                            Decimal xdec, ydec;
                            
                            cmd.Connection = connPgSql;
                            int k=0;

                            while (reader.Read())
                            {
                                k++;
                                tw.Progress("Copiando - " + prg.TabelaDestino, "<" + k + " de " + qtd + ">");
                    
                                npt = (int)reader.GetDecimal(2);
                                byte[] spdata = (byte[])reader["spatial_data"];
                                IList<NpgsqlPoint> points = new List<NpgsqlPoint>();

                                for (int i = 0; i < 2 * npt; i = i + 2)
                                {
                                    x = BitConverter.ToDouble(spdata, 8 * i);
                                    xdec = (decimal)x;
                                    y = BitConverter.ToDouble(spdata, 8 * (i + 1));
                                    ydec = (decimal)y;
                                    points.Add(new NpgsqlPoint((float)xdec, (float)ydec));
                                }

                                var param = cmd.CreateParameter();
                                param.Direction = ParameterDirection.Input;
                                param.ParameterName = "geom_id";
                                param.Value = reader.GetInt32(0);
                                cmd.Parameters.Add(param);

                                var param1 = cmd.CreateParameter();
                                param1.Direction = ParameterDirection.Input;
                                param1.ParameterName = "object_id";
                                param1.Value = reader.GetString(1);
                                cmd.Parameters.Add(param1);

                                var param2 = cmd.CreateParameter();
                                param2.Direction = ParameterDirection.Input;
                                param2.ParameterName = "num_coords";
                                param2.Value = (int)reader.GetDecimal(2);
                                cmd.Parameters.Add(param2);

                                var param3 = cmd.CreateParameter();
                                param3.Direction = ParameterDirection.Input;
                                param3.ParameterName = "num_holes";
                                param3.Value = (int)reader.GetDecimal(3);
                                cmd.Parameters.Add(param3);

                                var param4 = cmd.CreateParameter();
                                param4.Direction = ParameterDirection.Input;
                                param4.ParameterName = "parent_id";
                                param4.Value = reader.GetInt32(4);
                                cmd.Parameters.Add(param4);

                                NpgsqlTypes.NpgsqlBox box = new NpgsqlTypes.NpgsqlBox(new NpgsqlPoint((float)reader.GetDouble(7),(float)reader.GetDouble(8)), new NpgsqlPoint((float)reader.GetDouble(5), (float)reader.GetDouble(6)));
                                var param5 = cmd.CreateParameter();
                                param5.Direction = ParameterDirection.Input;
                                param5.ParameterName = "spatial_box";
                                param5.Value = box;
                                cmd.Parameters.Add(param5);

                                var param6 = cmd.CreateParameter();
                                param6.Direction = ParameterDirection.Input;
                                param6.ParameterName = "ext_max";
                                param6.Value = reader.GetDouble(9);
                                cmd.Parameters.Add(param6);

                                NpgsqlTypes.NpgsqlPolygon polygon = new NpgsqlTypes.NpgsqlPolygon(points.ToArray<NpgsqlPoint>());

                                var param7 = cmd.CreateParameter();
                                param7.Direction = ParameterDirection.Input;
                                param7.ParameterName = "spatial_data";
                                param7.Value = polygon;
                                cmd.Parameters.Add(param7);
                       
                                String sqlInsert = "Insert into " + TabelaDestino + "(geom_id, object_id, num_coords, num_holes, parent_id, spatial_box,ext_max, spatial_data) values(";
                                sqlInsert += ":geom_id,:object_id,:num_coords,:num_holes,:parent_id,:spatial_box,:ext_max,:spatial_data)";
                       
                                try
                                {
                                    cmd.CommandText = sqlInsert;
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    prg.Log(ex.ToString());
                                }
                            }
                            reader.Close();
                        }
                    }
                }

                tw.Close();
                prg.Log("Finalizando Conversão...");
            }
            catch (Exception ex)
            {
                tw.Close();
                prg.Log(ex.ToString());
            }
        }

        private void CopiarTabelaLinha(Program prg)
        {
            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                prg.Log("Copiando Tabela");

                strConnSQL = "Server=" + ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                strConnPgSql = "server=" + ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
                string consulta = "";
                Int32 npt = 0;
                Int32 qtd = 0;
                
                prg.Log(strConnSQL);
                prg.Log(strConnPgSql);

                NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql);
                connPgSql.Open();

                if (Apagar)
                {
                    prg.Log("Apagando Registros");
                    prg.Log("Tabela - " + prg.TabelaDestino);

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        try
                        {
                            cmd.Connection = connPgSql;
                            cmd.CommandText = "delete from " + TabelaDestino;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            prg.Log(ex.ToString());
                        }
                    }

                    prg.Log("Tabela - " + TabelaDestino + " - Registros Apagados");
                }

                prg.Log("select * from " + TabelaDestino + " where 1=2");

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();

                    consulta = "select count(*) as qtd from " + prg.TabelaOrigem;
                    prg.Log(consulta);
                    SqlCommand cmdCount = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = cmdCount.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            qtd = Convert.ToInt32(reader["qtd"]);
                            prg.Log(qtd + " registros...");
                        }
                    }

                    consulta = "select * from " + TabelaOrigem;
                    SqlCommand command = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand())
                        {
                            Double x, y;
                            Decimal xdec, ydec;

                            cmd.Connection = connPgSql;

                            int k = 0;

                            while (reader.Read())
                            {
                                k++;
                                tw.Progress("Copiando - " + prg.TabelaDestino, "<" + k + " de " + qtd + ">");
                                
                                npt = (int)reader.GetDecimal(2);
                                byte[] spdata = (byte[])reader["spatial_data"];
                                IList<NpgsqlPoint> points = new List<NpgsqlPoint>();

                                for (int i = 0; i < 2 * npt; i = i + 2)
                                {
                                    x = BitConverter.ToDouble(spdata, 8 * i);
                                    xdec = (decimal)x;
                                    y = BitConverter.ToDouble(spdata, 8 * (i + 1));
                                    ydec = (decimal)y;
                                    points.Add(new NpgsqlPoint((float)xdec, (float)ydec));
                                }

                                var param = cmd.CreateParameter();
                                param.Direction = ParameterDirection.Input;
                                param.ParameterName = "geom_id";
                                param.Value = reader.GetInt32(0);
                                cmd.Parameters.Add(param);

                                var param1 = cmd.CreateParameter();
                                param1.Direction = ParameterDirection.Input;
                                param1.ParameterName = "object_id";
                                param1.Value = reader.GetString(1);
                                cmd.Parameters.Add(param1);

                                var param2 = cmd.CreateParameter();
                                param2.Direction = ParameterDirection.Input;
                                param2.ParameterName = "num_coords";
                                param2.Value = (int)reader.GetDecimal(2);
                                cmd.Parameters.Add(param2);
                                
                                NpgsqlTypes.NpgsqlBox box = new NpgsqlTypes.NpgsqlBox(new NpgsqlPoint((float)reader.GetDouble(5), (float)reader.GetDouble(6)), new NpgsqlPoint((float)reader.GetDouble(3), (float)reader.GetDouble(4)));
                                var param5 = cmd.CreateParameter();
                                param5.Direction = ParameterDirection.Input;
                                param5.ParameterName = "spatial_box";
                                param5.Value = box;
                                cmd.Parameters.Add(param5);

                                var param6 = cmd.CreateParameter();
                                param6.Direction = ParameterDirection.Input;
                                param6.ParameterName = "ext_max";
                                param6.Value = reader.GetDouble(7);
                                cmd.Parameters.Add(param6);

                                NpgsqlTypes.NpgsqlPolygon polygon = new NpgsqlTypes.NpgsqlPolygon(points.ToArray<NpgsqlPoint>());

                                var param7 = cmd.CreateParameter();
                                param7.Direction = ParameterDirection.Input;
                                param7.ParameterName = "spatial_data";
                                param7.Value = polygon;
                                cmd.Parameters.Add(param7);

                                String sqlInsert = "Insert into " + TabelaDestino + "(geom_id, object_id, num_coords, spatial_box,ext_max, spatial_data) values(";
                                sqlInsert += ":geom_id,:object_id,:num_coords,:spatial_box,:ext_max,:spatial_data)";

                                try
                                {
                                    cmd.CommandText = sqlInsert;
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    prg.Log(ex.ToString());
                                }
                            }
                            reader.Close();
                        }
                    }
                }

                prg.Log("Finalizando Conversão...");
                tw.Close();
            }
            catch (Exception ex)
            {
                tw.Close();
                prg.Log(ex.ToString());
            }
        }

        private void CopiarTabelaPonto(Program prg)
        {
            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                prg.Log("Copiando Tabela");

                strConnSQL = "Server=" + ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                strConnPgSql = "server=" + ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
                string consulta = "";
                Int32 qtd = 0;

                prg.Log(strConnSQL);
                prg.Log(strConnPgSql);

                NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql);
                connPgSql.Open();

                if (Apagar)
                {
                    prg.Log("Apagando Registros");
                    prg.Log("Tabela - " + prg.TabelaDestino);

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        try
                        {
                            cmd.Connection = connPgSql;
                            cmd.CommandText = "delete from " + prg.TabelaDestino;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            prg.Log(ex.ToString());
                        }
                    }

                    prg.Log("Tabela - " + prg.TabelaDestino + " - Registros Apagados");
                }

                prg.Log("select * from " + prg.TabelaDestino + " where 1=2");

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();

                    consulta = "select count(*) as qtd from " + prg.TabelaOrigem;
                    prg.Log(consulta);
                    SqlCommand cmdCount = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = cmdCount.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            qtd = Convert.ToInt32(reader["qtd"]);
                            prg.Log(qtd + " registros...");
                        }
                    }

                    consulta = "select * from " + prg.TabelaOrigem;
                    SqlCommand command = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand())
                        {
                            cmd.Connection = connPgSql;
                            
                            int k = 0;

                            while (reader.Read())
                            {
                                k++;
                                tw.Progress("Copiando - " + prg.TabelaDestino, "<" + k + " de " + qtd + ">");
                                
                                var param = cmd.CreateParameter();
                                param.Direction = ParameterDirection.Input;
                                param.ParameterName = "geom_id";
                                param.Value = reader.GetInt32(0);
                                cmd.Parameters.Add(param);

                                var param1 = cmd.CreateParameter();
                                param1.Direction = ParameterDirection.Input;
                                param1.ParameterName = "object_id";
                                param1.Value = reader.GetString(1);
                                cmd.Parameters.Add(param1);

                                NpgsqlTypes.NpgsqlBox box = new NpgsqlTypes.NpgsqlBox(new NpgsqlPoint((float)reader.GetDouble(3), (float)reader.GetDouble(4)), new NpgsqlPoint((float)reader.GetDouble(3), (float)reader.GetDouble(4)));
                                var param5 = cmd.CreateParameter();
                                param5.Direction = ParameterDirection.Input;
                                param5.ParameterName = "spatial_box";
                                param5.Value = box;
                                cmd.Parameters.Add(param5);

                                var param6 = cmd.CreateParameter();
                                param6.Direction = ParameterDirection.Input;
                                param6.ParameterName = "x";
                                param6.Value = reader.GetDouble(3);
                                cmd.Parameters.Add(param6);

                                var param7 = cmd.CreateParameter();
                                param7.Direction = ParameterDirection.Input;
                                param7.ParameterName = "y";
                                param7.Value = reader.GetDouble(4);
                                cmd.Parameters.Add(param7);

                                String sqlInsert = "Insert into " + TabelaDestino + "(geom_id, object_id, spatial_box, x, y) values(";
                                sqlInsert += ":geom_id,:object_id,:spatial_box,:x,:y)";

                                try
                                {
                                    cmd.CommandText = sqlInsert;
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    prg.Log(ex.ToString());
                                }
                            }
                            reader.Close();
                        }
                    }
                }

                prg.Log("Finalizando Conversão...");
                tw.Close();
            }
            catch (Exception ex)
            {
                tw.Close();
                prg.Log(ex.ToString());
            }
        }

        private void CopiarTabelaTexto(Program prg)
        {
            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                prg.Log("Copiando Tabela");

                strConnSQL = "Server=" + ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                strConnPgSql = "server=" + ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
                string consulta = "";
                Int32 qtd = 0;

                prg.Log(strConnSQL);
                prg.Log(strConnPgSql);

                NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql);
                connPgSql.Open();

                if (Apagar)
                {
                    prg.Log("Apagando Registros");
                    prg.Log("Tabela - " + prg.TabelaDestino);

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        try
                        {
                            cmd.Connection = connPgSql;
                            cmd.CommandText = "delete from " + prg.TabelaDestino;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            prg.Log(ex.ToString());
                        }
                    }

                    prg.Log("Tabela - " + prg.TabelaDestino + " - Registros Apagados");
                }

                prg.Log("select * from " + prg.TabelaDestino + " where 1=2");

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();

                    consulta = "select count(*) as qtd from " + prg.TabelaOrigem;
                    prg.Log(consulta);
                    SqlCommand cmdCount = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = cmdCount.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            qtd = Convert.ToInt32(reader["qtd"]);
                            prg.Log(qtd + " registros...");
                        }
                    }

                    consulta = "select * from " + prg.TabelaOrigem;
                    SqlCommand command = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand())
                        {
                            cmd.Connection = connPgSql;

                            int k = 0;

                            while (reader.Read())
                            {
                                k++;
                                tw.Progress("Copiando - " + prg.TabelaDestino, "<" + k + " de " + qtd + ">");
                                
                                var param = cmd.CreateParameter();
                                param.Direction = ParameterDirection.Input;
                                param.ParameterName = "geom_id";
                                param.Value = reader.GetInt32(0);
                                cmd.Parameters.Add(param);

                                var param1 = cmd.CreateParameter();
                                param1.Direction = ParameterDirection.Input;
                                param1.ParameterName = "object_id";
                                param1.Value = reader.GetString(1);
                                cmd.Parameters.Add(param1);

                                NpgsqlTypes.NpgsqlBox box = new NpgsqlTypes.NpgsqlBox(new NpgsqlPoint((float)reader.GetDouble(2), (float)reader.GetDouble(3)), new NpgsqlPoint((float)reader.GetDouble(2), (float)reader.GetDouble(3)));
                                var param2 = cmd.CreateParameter();
                                param2.Direction = ParameterDirection.Input;
                                param2.ParameterName = "spatial_box";
                                param2.Value = box;
                                cmd.Parameters.Add(param2);

                                var param3 = cmd.CreateParameter();
                                param3.Direction = ParameterDirection.Input;
                                param3.ParameterName = "x";
                                param3.Value = reader.GetDouble(2);
                                cmd.Parameters.Add(param3);

                                var param4 = cmd.CreateParameter();
                                param4.Direction = ParameterDirection.Input;
                                param4.ParameterName = "y";
                                param4.Value = reader.GetDouble(3);
                                cmd.Parameters.Add(param4);

                                var param5 = cmd.CreateParameter();
                                param5.Direction = ParameterDirection.Input;
                                param5.ParameterName = "text_value";
                                param5.Value = reader.GetString(4);
                                cmd.Parameters.Add(param5);

                                var param6 = cmd.CreateParameter();
                                param6.Direction = ParameterDirection.Input;
                                param6.ParameterName = "angle";
                                param6.Value = reader.GetDouble(5);
                                cmd.Parameters.Add(param6);

                                var param7 = cmd.CreateParameter();
                                param7.Direction = ParameterDirection.Input;
                                param7.ParameterName = "height";
                                param7.Value = reader.GetDouble(6);
                                cmd.Parameters.Add(param7);

                                var param8 = cmd.CreateParameter();
                                param8.Direction = ParameterDirection.Input;
                                param8.ParameterName = "alignment_vert";
                                param8.Value = reader.GetDouble(7);
                                cmd.Parameters.Add(param8);

                                var param9 = cmd.CreateParameter();
                                param9.Direction = ParameterDirection.Input;
                                param9.ParameterName = "alignment_horiz";
                                param9.Value = reader.GetDouble(8);
                                cmd.Parameters.Add(param9);

                                String sqlInsert = "Insert into " + TabelaDestino + "(geom_id, object_id, spatial_box, x, y,text_value,angle,alignment_vert,alignment_horiz) values(";
                                sqlInsert += ":geom_id,:object_id,:spatial_box,:x,:y,:text_value,:angle,:alignment_vert,:alignment_horiz)";

                                try
                                {
                                    cmd.CommandText = sqlInsert;
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex)
                                {
                                    prg.Log(ex.ToString());
                                }
                            }
                            reader.Close();
                        }
                    }
                }

                prg.Log("Finalizando Conversão...");
                tw.Close();
            }
            catch (Exception ex)
            {
                tw.Close();
                prg.Log(ex.ToString());
            }
        }
        
        public bool Apagar
        {
            set
            {
                gApagar = value;
            }
            get
            {
                return gApagar;
            }
        }

        public int TipoRepresentacao
        {
            set
            {
                gTipoRepresentacao = value;
            }
            get
            {
                return gTipoRepresentacao;
            }
        }

        private void CopiarTabela(Program prg)
        {
            switch (prg.TipoRepresentacao)
            {
                case 1:
                    CopiarTabelaPolygono(prg);
                    break;

                case 2:
                    CopiarTabelaLinha(prg);
                    break;

                case 4:
                    CopiarTabelaPonto(prg);
                    break;

                case 128:
                    CopiarTabelaTexto(prg);
                    break;

                case 512:
                    break;
            }
        }
    }
}
