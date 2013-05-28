using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using System.Drawing;
using System.IO;

namespace MJr_Postgres2Terralib
{
    public class Program
    {
        private string gCaminhoBancoMDB = "", gServidorPostgreSQL = "";
        private string gTabelaDestino = "", gTabelaOrigem = "", gCondicao = "";
        private bool gApagar = false;
        
        static void Main(string[] args)
        {
            Program prg = new Program();

            try
            {
                int i = 0;
                prg.Log("MJr - PostgreSQL to Terralib - Entrando no Sistema");
         
                foreach (string arg in args)
                {
                    if (i == 0)
                        prg.ServidorPostgreSQL = arg;
                    if (i == 1)
                        prg.CaminhoBancoMDB = arg;
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
                        prg.Condicao = arg;
                    
                    i++;
                }

                prg.Log("Servidor PostgreSQL - " + prg.ServidorPostgreSQL);
                prg.Log("Caminho MDB - " + prg.CaminhoBancoMDB);
                prg.Log("Tabela Origem - " + prg.TabelaOrigem);
                prg.Log("Tabela Destino - " + prg.TabelaDestino);
                prg.Log("Apagar - " + prg.Apagar.ToString());
                prg.Log("Condição - " + prg.Condicao.ToString());

                if (prg.TabelaDestino.Length > 0)
                    if (prg.TabelaOrigem.Length > 0)
                        prg.CopiarTabela(prg, prg.TabelaOrigem, prg.TabelaDestino);

                prg.Log("MJr - PostgreSQL to Terralib - Saindo do Sistema...");
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
                FileStream arquivo = new FileStream(diretorio + "\\MJr-PostgreSQL2Access.txt", FileMode.Append, FileAccess.Write, FileShare.None);
                StreamWriter sw = new StreamWriter(arquivo);
                sw.WriteLine(texto);
                sw.Close();
                arquivo.Close();
            }
            catch (Exception ex)
            {
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

        public string CaminhoBancoMDB
        {
            set
            {
                gCaminhoBancoMDB = value;
            }
            get
            {
                return gCaminhoBancoMDB;
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

        public string Condicao
        {
            set
            {
                gCondicao = value;
            }
            get
            {
                return gCondicao;
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

        private void CopiarTabela(Program prg, string OrigemTabela, string DestinoTabela)
        {
            try
            {
                prg.Log("Copiando Tabela");

                string strConnPgSql = "server=" + ServidorPostgreSQL + ";port=5432;user id=sigriodosul;password=sigriodosul;database=sigriodosul;Preload Reader=true;";
                string strConnMDB = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + CaminhoBancoMDB + "\\TERRALIB20.mdb;Persist Security Info=True";
                string consulta = "";
                Int32 npt = 0;
                object RecordsAffected;
                int iCount = 0;

                prg.Log(strConnPgSql);
                prg.Log(strConnMDB);

                ADODB.Connection connMDB = new ADODB.Connection();
                ADODB.Recordset rsMDB = new ADODB.Recordset();

                connMDB.Open(strConnMDB);

                if (Apagar)
                {
                    prg.Log("Apagando Registros");
                    prg.Log("Tabela - " + DestinoTabela); 
                    connMDB.Execute("delete from " + DestinoTabela, out RecordsAffected);
                    prg.Log("Tabela - " + DestinoTabela + " - Registros Apagados - " + RecordsAffected); 
                }

                prg.Log("select * from " + DestinoTabela + " where 1=2");
                rsMDB.Open("select * from " + DestinoTabela + " where 1=2", connMDB, ADODB.CursorTypeEnum.adOpenDynamic, ADODB.LockTypeEnum.adLockOptimistic);

                using (NpgsqlConnection connPgSql = new NpgsqlConnection(strConnPgSql))
                {
                    connPgSql.Open();

                    consulta = "select geom_id, object_id, num_coords, num_holes, ";
                    consulta += " parent_id,(spatial_box[0])[0] as lower_x,(spatial_box[0])[1] as lower_y,(spatial_box[1])[0] as upper_x, (spatial_box[1])[1] as upper_y,";
                    consulta += " ext_max, spatial_data";
                    consulta += " from " + OrigemTabela;

                    if (prg.Condicao.Length > 0)
                        consulta += " where " + prg.Condicao;

                    prg.Log(consulta);

                    NpgsqlCommand command = new NpgsqlCommand(consulta, connPgSql);

                    prg.Log("Convertendo...");

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            iCount++;
                            prg.Log("...<" + iCount + ">.<" + reader["object_id"] + ">");

                            NpgsqlTypes.NpgsqlPolygon spdata = (NpgsqlTypes.NpgsqlPolygon)reader["spatial_data"];

                            List<PointF> vertices = new List<PointF>();

                            foreach (NpgsqlTypes.NpgsqlPoint point in spdata)
                            {
                                vertices.Add(new PointF((float)point.X, (float)point.Y));
                            }

                            npt = (Int32)(reader["num_coords"]);
                            double[] dArray = new double[2 * npt];
                            int j = 0;

                            for (int i = 0; i < 2 * npt; i = i + 2)
                            {
                                dArray[i] = vertices[j].X;
                                dArray[i + 1] = vertices[j].Y;
                                j++;
                            }

                            var result = new byte[dArray.Length * sizeof(double)];
                            Buffer.BlockCopy(dArray, 0, result, 0, result.Length);
                            byte[] spdataOLE = result;

                            rsMDB.AddNew();
                            rsMDB.Fields["object_id"].Value = reader["object_id"];
                            rsMDB.Fields["num_coords"].Value = reader["num_coords"];
                            rsMDB.Fields["num_holes"].Value = reader["num_holes"];
                            rsMDB.Fields["parent_id"].Value = reader["parent_id"];
                            rsMDB.Fields["lower_x"].Value = reader["lower_x"];
                            rsMDB.Fields["lower_y"].Value = reader["lower_y"];
                            rsMDB.Fields["upper_x"].Value = reader["upper_x"];
                            rsMDB.Fields["upper_y"].Value = reader["upper_y"];
                            rsMDB.Fields["ext_max"].Value = reader["ext_max"];
                            rsMDB.Fields["spatial_data"].AppendChunk(spdataOLE);
                            rsMDB.Update();
                        }
                    }
                }

                prg.Log("Finalizando Conversão...");

            }
            catch (Exception ex)
            {
                prg.Log(ex.ToString());
            }
        }
    }
}
