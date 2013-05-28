using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Npgsql;
using System.Drawing;
using System.IO;
using System.Data.SqlClient;
using System.Data;
using NpgsqlTypes;

namespace MJr_SaoLuisUpdate
{
    public class Program
    {
        private string gServidorPostgreSQL = "";
        private string gServidorSQLServer = "";
        private string cnsPathExe = "";
        private string cnsConfigDB = "";
        
        static void Main(string[] args)
        {
            Program prg = new Program();
            int i = 0;
            int LayerId = 0;
            try
            {
                foreach (string arg in args)
                {
                    if (i == 0)
                        LayerId = Convert.ToInt32(arg);
                    i++;
                }

                prg.PathExe = "E:\\GitHub\\codigos\\MJr-SQLServer2PostgreSQL\\MJr-SQLServer2PostgreSQL\\bin\\Debug\\MJr-SQLServer2PostgreSQL.exe";
                prg.ConfigDB = "(local) localhost ";
                prg.Log("MJr - São Luis Update - Entrando no Sistema");
                prg.ServidorPostgreSQL = "localhost";
                prg.ServidorSQLServer = "(local)";

                string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                string consulta = "";

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();
                    consulta = "select l.layer_id, l.name, t.attr_table, t.unique_id, r.geom_table, r.geom_type, l.lower_x, l.lower_y, l.upper_x, l.upper_y ";
                    consulta += "from te_layer l ";
                    consulta += "inner join te_layer_table t on t.layer_id = l.layer_id ";
                    consulta += "inner join te_representation r on r.layer_id = l.layer_id ";
                    consulta += "where r.geom_type not in (512) ";
                    consulta += "and l.layer_id >= " + LayerId + " ";
                    consulta += "order by l.layer_id, l.name, r.geom_table ";

                    SqlCommand command = new SqlCommand(consulta, connSql);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        TelaWait tw = new TelaWait();
                        tw.Show();

                        while (reader.Read())
                        {
                            prg.Log("START - Copiando tabela - " + (string)reader["geom_table"].ToString());
                            
                            tw.Progress("Atualizando...", "Criando layer - " + reader["name"].ToString());
                            int lLayerId = prg.AdicionarLayer(prg, reader["name"].ToString().ToLower(), Convert.ToInt32(reader["geom_type"]), Convert.ToDouble(reader["lower_x"]), Convert.ToDouble(reader["lower_y"]), Convert.ToDouble(reader["upper_x"]), Convert.ToDouble(reader["upper_y"]));

                            prg.Log("Adicionando Layer Table - " + "geo_" + reader["name"].ToString().ToLower() + " - " + "object_id_" + lLayerId);
                            prg.AdicionarLayerTable(prg, lLayerId, "geo_" + reader["name"].ToString().ToLower(), "object_id_" + lLayerId);

                            tw.Progress("Atualizando...", "Criando estrutura - " + reader["geom_table"].ToString());
                            prg.CriarEstrutura(prg, Convert.ToInt32(reader["geom_type"]), lLayerId);
                            
                            tw.Progress("Atualizando...", "Copiando tabela geometria - " + reader["geom_table"].ToString());
                            prg.CopiarTabela(prg, reader["geom_table"].ToString(), reader["geom_table"].ToString(), Convert.ToInt32(reader["geom_type"]));

                            tw.Progress("Atualizando...", "Copiando tabela atributo - " + reader["attr_table"].ToString());
                            prg.CopiarTabelaAtributo(prg, reader["attr_table"].ToString(), "geo_" + reader["name"].ToString().ToLower(), reader["unique_id"].ToString().ToLower(), "object_id_" + lLayerId);
                            
                            prg.Log("END - Copiando tabela - " + (string)reader["geom_table"]);
                        }

                        tw.Close();
                    }
                }
                
                prg.Log("MJr - São Luis Update - Saindo do Sistema...");
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
                FileStream arquivo = new FileStream(diretorio + "\\MJr-SaoLuisUpdate.txt", FileMode.Append, FileAccess.Write, FileShare.None);
                StreamWriter sw = new StreamWriter(arquivo);
                sw.WriteLine(texto);
                sw.Close();
                arquivo.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ShellAndWait(string myFileName, string myArguments)
        {
            System.Diagnostics.ProcessStartInfo myProcessInfo = new System.Diagnostics.ProcessStartInfo();
            myProcessInfo.FileName = myFileName;
            myProcessInfo.Arguments = myArguments;
            System.Diagnostics.Process myProcess = System.Diagnostics.Process.Start(myProcessInfo);
            myProcess.WaitForExit();
        }

        public string PathExe
        {
            set
            {
                cnsPathExe = value;
            }
            get
            {
                return cnsPathExe;
            }
        }

        public string ConfigDB
        {
            set
            {
                cnsConfigDB = value;
            }
            get
            {
                return cnsConfigDB;
            }
        }

        private void CopiarTabela(Program prg, string OrigemTabela, string DestinoTabela, int Representacao)
        {
            try
            {
                prg.Log("Copiando Tabela...");
                prg.ShellAndWait(prg.PathExe, prg.ConfigDB + OrigemTabela + " " + DestinoTabela + " 1 " + Representacao);
                prg.Log("Finalizando Conversão...");
            }
            catch (Exception ex)
            {
                prg.Log(ex.ToString());
            }
        }

        private void CopiarTabelaAtributo(Program prg, string OrigemTabela, string DestinoTabela, string chaveOrigem, string chaveDestino)
        {
            string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
            string consulta = "";

            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                prg.Log("Copiando Tabela Atributo...");
                consulta = "create table " + DestinoTabela + " ( " + chaveDestino + " varchar(100))";
                prg.ExecutarConsulta(prg, consulta);
                Int32 qtd=0;

                NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql);
                connPgSql.Open();

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();
                    consulta = "select count(*) as qtd from " + OrigemTabela;
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

                    consulta = "select * from " + OrigemTabela;
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
                                tw.Progress("Copiando - " + DestinoTabela, "<" + k + " de " + qtd + ">");
                                String sqlInsert = "Insert into " + DestinoTabela + "(" + chaveDestino + ") values(";
                                sqlInsert += "'" + reader[chaveOrigem].ToString() + "')";

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
                        }
                    }
                }

                tw.Close();
                prg.Log("Finalizando Conversão Atributo...");

            }
            catch (Exception ex)
            {
                tw.Close();
                prg.Log(ex.ToString());
            }
        }

        private void CriarEstrutura(Program prg, int Tipo, int LayerId)
        {
            try
            {
                prg.Log("Criando Estrutura...");

                string consulta = "";

                switch (Tipo)
                {
                    case 1:

                        consulta = "CREATE TABLE polygons" + LayerId + " ";
                        consulta += "( ";
                        consulta += "geom_id serial NOT NULL, ";
                        consulta += "object_id character varying(255) NOT NULL, ";
                        consulta += "num_coords integer NOT NULL, ";
                        consulta += "num_holes integer NOT NULL, ";
                        consulta += "parent_id integer NOT NULL, ";
                        consulta += "spatial_box box NOT NULL, ";
                        consulta += "ext_max double precision NOT NULL, ";
                        consulta += "spatial_data polygon, ";
                        consulta += "CONSTRAINT polygons" + LayerId + "_pkey PRIMARY KEY (geom_id) ";
                        consulta += ") ";
                        consulta += "WITH ( ";
                        consulta += "OIDS=FALSE ";
                        consulta += "); ";
                        prg.ExecutarConsulta(prg, consulta);
                        
                        consulta = "ALTER TABLE polygons" + LayerId + " OWNER TO sigsaoluis; ";
                        prg.ExecutarConsulta(prg, consulta);
                        
                        consulta = "CREATE INDEX te_idx_polygons" + LayerId + "_obj ";
                        consulta += "ON polygons" + LayerId + " ";
                        consulta += "USING btree ";
                        consulta += "(object_id); ";
                        prg.ExecutarConsulta(prg, consulta);
                        
                        break;

                    case 2:

                        consulta = "CREATE TABLE lines" + LayerId + " ";
                        consulta += "( ";
                        consulta += "geom_id serial NOT NULL, ";
                        consulta += "object_id character varying(255) NOT NULL, ";
                        consulta += "num_coords integer NOT NULL, ";
                        consulta += "spatial_box box NOT NULL, ";
                        consulta += "ext_max double precision NOT NULL, ";
                        consulta += "spatial_data polygon, ";
                        consulta += "CONSTRAINT lines" + LayerId + "_pkey PRIMARY KEY (geom_id) ";
                        consulta += ") ";
                        consulta += "WITH ( ";
                        consulta += "OIDS=FALSE ";
                        consulta += "); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "ALTER TABLE lines" + LayerId + " OWNER TO sigsaoluis; ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX sp_idx_gist_lines" + LayerId + " ";
                        consulta += "ON lines" + LayerId + " ";
                        consulta += "USING gist ";
                        consulta += "(spatial_box); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX te_idx_lines" + LayerId + "_obj ";
                        consulta += "ON lines" + LayerId + " ";
                        consulta += "USING btree ";
                        consulta += "(object_id); ";
                        prg.ExecutarConsulta(prg, consulta);

                        break;

                    case 4:

                        consulta = "CREATE TABLE points" + LayerId + " ";
                        consulta += "( ";
                        consulta += "geom_id serial NOT NULL, ";
                        consulta += "object_id character varying(255) NOT NULL, ";
                        consulta += "spatial_box box NOT NULL, ";
                        consulta += "x double precision DEFAULT 0.0, ";
                        consulta += "y double precision DEFAULT 0.0, ";
                        consulta += "CONSTRAINT points" + LayerId + "_pkey PRIMARY KEY (geom_id) ";
                        consulta += ") ";
                        consulta += "WITH ( ";
                        consulta += "OIDS=FALSE ";
                        consulta += "); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "ALTER TABLE points" + LayerId + " OWNER TO sigsaoluis; ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX sp_idx_gist_points" + LayerId + " ";
                        consulta += "ON points" + LayerId + " ";
                        consulta += "USING gist ";
                        consulta += "(spatial_box); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX te_idx_points" + LayerId + "_obj ";
                        consulta += "ON points" + LayerId + " ";
                        consulta += "USING btree ";
                        consulta += "(object_id); ";
                        prg.ExecutarConsulta(prg, consulta);

                        break;

                    case 128:

                        consulta = "CREATE TABLE texts" + LayerId + " ";
                        consulta += "( ";
                        consulta += "geom_id serial NOT NULL, ";
                        consulta += "object_id character varying(255) NOT NULL, ";
                        consulta += "spatial_box box NOT NULL, ";
                        consulta += "x double precision DEFAULT 0.0, ";
                        consulta += "y double precision DEFAULT 0.0, ";
                        consulta += "text_value character varying(255) NOT NULL, ";
                        consulta += "angle double precision DEFAULT 0.0, ";
                        consulta += "height double precision DEFAULT 0.0, ";
                        consulta += "alignment_vert double precision DEFAULT 0.0, ";
                        consulta += "alignment_horiz double precision DEFAULT 0.0, ";
                        consulta += "CONSTRAINT texts" + LayerId + "_pkey PRIMARY KEY (geom_id) ";
                        consulta += ") ";
                        consulta += "WITH ( ";
                        consulta += "OIDS=FALSE ";
                        consulta += "); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "ALTER TABLE texts" + LayerId + " OWNER TO sigsaoluis; ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX sp_idx_gist_texts" + LayerId + " ";
                        consulta += "ON texts" + LayerId + " ";
                        consulta += "USING gist ";
                        consulta += "(spatial_box); ";
                        prg.ExecutarConsulta(prg, consulta);

                        consulta = "CREATE INDEX te_idx_texts" + LayerId + "_obj ";
                        consulta += "ON texts" + LayerId + " ";
                        consulta += "USING btree ";
                        consulta += "(object_id); ";
                        prg.ExecutarConsulta(prg, consulta);

                        break;

                    case 512:
                        break;
                }

                prg.Log("Finalizando Estrutura...");
            }
            catch (Exception ex)
            {
                prg.Log(ex.ToString());
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

        public void ExecutarConsulta(Program prg, string sql)
        {
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";

            using (Npgsql.NpgsqlConnection conn = new NpgsqlConnection(strConnPgSql))
            {
                conn.Open();

                using (Npgsql.NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    try
                    {
                        prg.Log("CONSULTA - " + sql);
                        cmd.Connection = conn;
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        prg.Log(ex.ToString());
                    }
                }
            }
        }

        public int AdicionarLayer(Program prg, string nome, int representacao, double xmin, double ymin, double xmax, double ymax)
        {
            string consulta = "";
            int lProjectionId = AdicionarProjection(prg);
            int lLayerId = 0;
            string lGeomTable = "";

            consulta  = "INSERT INTO te_layer( ";
            consulta += "projection_id, name, lower_x, lower_y, upper_x, upper_y,  ";
            consulta += "initial_time, final_time, edition_time) ";
            consulta += "VALUES (" + lProjectionId + ",'" + nome + "', " + xmin.ToString().Replace(",", ".") + "," + ymin.ToString().Replace(",", ".") + "," + xmax.ToString().Replace(",", ".") + "," + ymax.ToString().Replace(",", ".") + ", ";
            consulta += "null, null, null); ";
            prg.ExecutarConsulta(prg,consulta);

            lLayerId = prg.RetornarValorMaximo(prg, "te_layer", "layer_id");

            switch (representacao)
            {
                case 1:
                    lGeomTable = "polygons" + lLayerId;
                    break;
                case 2:
                    lGeomTable = "lines" + lLayerId;
                    break;
                case 4:
                    lGeomTable = "points" + lLayerId;
                    break;
                case 128:
                    lGeomTable = "texts" + lLayerId;
                    break;
            }

            consulta = "INSERT INTO te_representation( ";
            consulta += "layer_id, geom_type, geom_table, description, lower_x,  ";
            consulta += "lower_y, upper_x, upper_y, res_x, res_y, num_cols, num_rows,  ";
            consulta += "initial_time, final_time) ";
            consulta += "VALUES (" + lLayerId + ", " + representacao + ", '" + lGeomTable + "', '', " + xmin.ToString().Replace(",",".") + ", ";
            consulta += "" + ymin.ToString().Replace(",", ".") + ", " + xmax.ToString().Replace(",", ".") + ", " + ymax.ToString().Replace(",", ".") + ", null, null, null, null, ";
            consulta += "null, null); ";
            prg.ExecutarConsulta(prg, consulta);

            return lLayerId;
        }

        public int AdicionarProjection(Program prg)
        {
            string consulta = "";
            int lProjectionId = 0;
            
            consulta = "INSERT INTO te_projection( ";
            consulta += "name, long0, lat0, offx, offy, stlat1, stlat2,  ";
            consulta += "unit, scale, hemis, datum) ";
            consulta += "VALUES ('UTM', -45, 0, 500000, 10000000, 0, 0, ";
            consulta += "'Meters', 0.9996, 1, 'WGS84'); ";
            prg.ExecutarConsulta(prg, consulta);

            lProjectionId = prg.RetornarValorMaximo(prg, "te_projection", "projection_id");

            consulta = "INSERT INTO te_srs( ";
            consulta += "projection_id, srs_id) ";
            consulta += "VALUES (" + lProjectionId + ", -1); ";
            prg.ExecutarConsulta(prg, consulta);

            return lProjectionId;
        }

        public int RetornarValorMaximo(Program prg, string Tabela, string Campo)
        {
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";

            using (Npgsql.NpgsqlConnection conn = new NpgsqlConnection(strConnPgSql))
            {
                conn.Open();

                using (Npgsql.NpgsqlCommand cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select max(" + Campo + ") as valor from " + Tabela;
                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            return Convert.ToInt32(reader["valor"]);
                        }
                    }
                }
            }

            return 0;
        }

        public void AdicionarLayerTable(Program prg, int layerid, string nome, string chave)
        {
            string consulta = "";

            consulta = "INSERT INTO te_layer_table( ";
            consulta += "layer_id, attr_table, unique_id, attr_link, attr_initial_time,  ";
            consulta += "attr_final_time, attr_time_unit, attr_table_type, user_name,  ";
            consulta += "initial_time, final_time) ";
            consulta += "VALUES (" + layerid + ", '" + nome + "', '" + chave + "', '" + chave + "', '', '', 1, 1, '', null, null)";
            prg.ExecutarConsulta(prg, consulta);
        }
    }
}
