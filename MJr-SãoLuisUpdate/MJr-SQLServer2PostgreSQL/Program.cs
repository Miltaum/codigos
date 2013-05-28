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
        private string gTabelaDestino = "", gTabelaOrigem = "";
        private string strConnSQL = "";
        private string strConnPgSql = "";
        private bool gApagar = false;
        private int gTipoRepresentacao = 0;
        private string gAndWhere = "";

        static void Main(string[] args)
        {
            Program prg = new Program();
            
            try
            {
                int i = 0;

                foreach (string arg in args)
                {
                    if (i == 0)
                        prg.AndWhere = Convert.ToString(arg);

                    i++;
                }

                prg.PathExe = "E:\\GitHub\\codigos\\MJr-SQLServer2PostgreSQL\\MJr-SQLServer2PostgreSQL\\bin\\Debug\\MJr-SQLServer2PostgreSQL.exe";
                prg.ConfigDB = "(local) localhost ";
                prg.Log("MJr - São Luis Update - Entrando no Sistema");
                prg.ServidorPostgreSQL = "localhost";
                prg.ServidorSQLServer = "(local)";
                prg.AndWhere = "and l.layer_id >= 162";

                string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
                string consulta = "";

                using (SqlConnection connSql = new SqlConnection(strConnSQL))
                {
                    connSql.Open();

                    consulta = "select distinct l.layer_id, l.name, t.attr_table, t.unique_id, r.geom_table, r.geom_type, l.lower_x, l.lower_y, l.upper_x, l.upper_y ";
                    consulta += "from te_layer l ";
                    consulta += "inner join te_layer_table t on t.layer_id = l.layer_id ";
                    consulta += "inner join te_representation r on r.layer_id = l.layer_id ";
                    consulta += "where r.geom_type not in (512) ";

                    if (prg.AndWhere.Length > 0)
                        consulta += prg.AndWhere + " ";

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
                            string layerName = reader["name"].ToString().ToLower().Replace("ç", "c").Replace("á","a").Replace("ã","a").Replace("í","i").Replace("ó","o").Replace("é","e").Replace("ú","u");
                            int lLayerId = prg.AdicionarLayer(prg, layerName, Convert.ToInt32(reader["geom_type"]), Convert.ToDouble(reader["lower_x"]), Convert.ToDouble(reader["lower_y"]), Convert.ToDouble(reader["upper_x"]), Convert.ToDouble(reader["upper_y"]));
                            
                            prg.Log("Adicionando Layer Table - " + "geo_" + reader["name"].ToString().ToLower() + " - " + "object_id_" + lLayerId);
                            prg.AdicionarLayerTable(prg, lLayerId, "geo_" + reader["name"].ToString().ToLower(), "object_id_" + lLayerId);

                            tw.Progress("Atualizando...", "Criando estrutura - " + reader["geom_table"].ToString());
                            prg.CriarEstrutura(prg, Convert.ToInt32(reader["geom_type"]), lLayerId);

                            tw.Progress("Atualizando...", "Copiando tabela geometria - " + reader["geom_table"].ToString());
                            tw.Hide();
                            prg.CopiarTabela(prg, reader["geom_table"].ToString(), prg.TabelaDestino, Convert.ToInt32(reader["geom_type"]));
                            tw.Show();

                            tw.Progress("Atualizando...", "Copiando tabela atributo - " + reader["attr_table"].ToString());
                            tw.Hide();
                            prg.CopiarTabelaAtributo(prg, reader["attr_table"].ToString(), "geo_" + layerName, reader["unique_id"].ToString().ToLower(), "object_id_" + lLayerId);
                            tw.Show();

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
                //MessageBox.Show(ex.ToString());
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
            prg.TabelaOrigem = OrigemTabela;
            prg.TabelaDestino = DestinoTabela;
            prg.TipoRepresentacao = Representacao;

            switch (Representacao)
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
          
        private void CopiarTabelaAtributo(Program prg, string OrigemTabela, string DestinoTabela, string chaveOrigem, string chaveDestino)
        {
            int offset = 1;
            int limit = 100;
            int maxrow = 0; 
            
            string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";
            string consulta = "";

            TelaWait tw = new TelaWait();
            tw.Show();

            try
            {
                consulta = "select count(*) as qtd from " + DestinoTabela;
                prg.Log(consulta);

                using (NpgsqlConnection connSql = new NpgsqlConnection(strConnPgSql))
                {
                    connSql.Open();

                    NpgsqlCommand cmdExiste = new NpgsqlCommand(consulta, connSql);

                    try
                    {
                        using (NpgsqlDataReader reader = cmdExiste.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        
                    }
                    
                }

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
                            maxrow = qtd;
                            prg.Log(qtd + " registros...");
                        }
                    }

                    while (offset <= maxrow)
                    {
                        consulta = "SELECT " + chaveOrigem + " ";
                        consulta += "FROM ( ";
                        consulta += "SELECT " + chaveOrigem + ", ROW_NUMBER() OVER (ORDER BY " + chaveOrigem + ") AS RowNum ";
                        consulta += "FROM " + OrigemTabela + " ";
                        consulta += ") AS TabelaOffset ";
                        consulta += "WHERE TabelaOffset.RowNum BETWEEN " + offset + " AND " + (offset + limit - 1) + " ";
                        
                        using (SqlCommand command = new SqlCommand(consulta, connSql))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand())
                                {
                                    cmd.Connection = connPgSql;

                                    int k = offset - 1;

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
                        offset += limit;
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

            string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";

            consulta = "select * from te_layer where name = '" + nome + "'";
            prg.Log(consulta);

            using (NpgsqlConnection connPostgreSql = new NpgsqlConnection(strConnPgSql))
            {
                connPostgreSql.Open();

                NpgsqlCommand cmdExiste = new NpgsqlCommand(consulta, connPostgreSql);

                using (NpgsqlDataReader  reader = cmdExiste.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        lLayerId = Convert.ToInt32(reader["layer_id"]);
                    
                    }
                    else
                    {
                        consulta = "INSERT INTO te_layer( ";
                        consulta += "projection_id, name, lower_x, lower_y, upper_x, upper_y,  ";
                        consulta += "initial_time, final_time, edition_time) ";
                        consulta += "VALUES (" + lProjectionId + ",'" + nome + "', " + xmin.ToString().Replace(",", ".") + "," + ymin.ToString().Replace(",", ".") + "," + xmax.ToString().Replace(",", ".") + "," + ymax.ToString().Replace(",", ".") + ", ";
                        consulta += "null, null, null); ";
                        prg.ExecutarConsulta(prg, consulta);

                        lLayerId = prg.RetornarValorMaximo(prg, "te_layer", "layer_id");
                    }
                }
            }

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
            prg.TabelaDestino = lGeomTable;

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
            string strConnSQL = "Server=" + prg.ServidorSQLServer + ";Database=sigsaoluis_dados;User Id=sa;Password=sa;";
            string strConnPgSql = "server=" + prg.ServidorPostgreSQL + ";port=5432;user id=sigsaoluis;password=sigsaoluis;database=sigsaoluis;Preload Reader=true;";

            consulta = "select * from te_layer_table where layer_id = " + layerid + "";
            prg.Log(consulta);

            using (NpgsqlConnection connSql = new NpgsqlConnection(strConnPgSql))
            {
                connSql.Open();

                NpgsqlCommand cmdExiste = new NpgsqlCommand(consulta, connSql);

                using (NpgsqlDataReader reader = cmdExiste.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return;
                    }
                }
            }

            consulta = "INSERT INTO te_layer_table( ";
            consulta += "layer_id, attr_table, unique_id, attr_link, attr_initial_time,  ";
            consulta += "attr_final_time, attr_time_unit, attr_table_type, user_name,  ";
            consulta += "initial_time, final_time) ";
            consulta += "VALUES (" + layerid + ", '" + nome + "', '" + chave + "', '" + chave + "', '', '', 1, 1, '', null, null)";
            prg.ExecutarConsulta(prg, consulta);
        }

        private void CopiarTabelaPolygono(Program prg)
        {
            int offset = 1;
            int limit = 100;
            int maxrow = 0;

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
                            maxrow = qtd;
                            prg.Log(qtd + " registros...");
                        }
                    }

                    while (offset <= maxrow)
                    {
                        consulta = "SELECT * ";
                        consulta += "FROM ( ";
                        consulta += "SELECT *, ROW_NUMBER() OVER (ORDER BY geom_id) AS RowNum ";
                        consulta += "FROM " + prg.TabelaOrigem + " ";
                        consulta += ") AS TabelaOffset ";
                        consulta += "WHERE TabelaOffset.RowNum BETWEEN " + offset + " AND " + (offset + limit - 1) + " ";

                        using (SqlCommand command = new SqlCommand(consulta, connSql))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand())
                                {
                                    Double x, y;
                                    Decimal xdec, ydec;

                                    cmd.Connection = connPgSql;

                                    int k = offset -1;

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

                                        NpgsqlTypes.NpgsqlBox box = new NpgsqlTypes.NpgsqlBox(new NpgsqlPoint((float)reader.GetDouble(7), (float)reader.GetDouble(8)), new NpgsqlPoint((float)reader.GetDouble(5), (float)reader.GetDouble(6)));
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
                        offset += limit;
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
            int offset = 1;
            int limit = 100;
            int maxrow = 0;

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
                            maxrow = qtd;
                            prg.Log(qtd + " registros...");
                        }
                    }

                    while (offset <= maxrow)
                    {
                        consulta = "SELECT * ";
                        consulta += "FROM ( ";
                        consulta += "SELECT *, ROW_NUMBER() OVER (ORDER BY geom_id) AS RowNum ";
                        consulta += "FROM " + prg.TabelaOrigem + " ";
                        consulta += ") AS TabelaOffset ";
                        consulta += "WHERE TabelaOffset.RowNum BETWEEN " + offset + " AND " + (offset + limit - 1) + " ";

                        using(SqlCommand command = new SqlCommand(consulta, connSql))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand())
                                {
                                    Double x, y;
                                    Decimal xdec, ydec;

                                    cmd.Connection = connPgSql;

                                    int k = offset - 1;

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
                        offset += limit;
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
            int offset = 1;
            int limit = 100;
            int maxrow = 0;

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
                            maxrow = qtd;
                            prg.Log(qtd + " registros...");
                        }
                    }

                    while (offset <= maxrow)
                    {
                        consulta = "SELECT * ";
                        consulta += "FROM ( ";
                        consulta += "SELECT *, ROW_NUMBER() OVER (ORDER BY geom_id) AS RowNum ";
                        consulta += "FROM " + prg.TabelaOrigem + " ";
                        consulta += ") AS TabelaOffset ";
                        consulta += "WHERE TabelaOffset.RowNum BETWEEN " + offset + " AND " + (offset + limit - 1) + " ";

                        using (SqlCommand command = new SqlCommand(consulta, connSql))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand())
                                {
                                    cmd.Connection = connPgSql;

                                    int k = offset - 1;

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
                        offset += limit;
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
            int offset = 1;
            int limit = 100;
            int maxrow = 0;

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
                            maxrow = qtd;
                            prg.Log(qtd + " registros...");
                        }
                    }

                    while (offset <= maxrow)
                    {
                        consulta = "SELECT * ";
                        consulta += "FROM ( ";
                        consulta += "SELECT *, ROW_NUMBER() OVER (ORDER BY geom_id) AS RowNum ";
                        consulta += "FROM " + prg.TabelaOrigem + " ";
                        consulta += ") AS TabelaOffset ";
                        consulta += "WHERE TabelaOffset.RowNum BETWEEN " + offset + " AND " + (offset + limit - 1) + " ";

                        using (SqlCommand command = new SqlCommand(consulta, connSql))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (NpgsqlCommand cmd = new NpgsqlCommand())
                                {
                                    cmd.Connection = connPgSql;

                                    int k = offset - 1;

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
                        offset += limit;
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

        public string AndWhere
        {
            set
            {
                gAndWhere = value;
            }
            get
            {
                return gAndWhere;
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
    }
}
