﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Tenaris.Fava.Production.Reporting.Model.DTO;
using Tenaris.Fava.Production.Reporting.Model.Model;
using Tenaris.Library.DbClient;
using Tenaris.Library.Framework.Utility.Conversion;
using Tenaris.Library.Log;


namespace Tenaris.Fava.Production.Reporting.Model.Data_Access
{
    public class DataAccessSQL
    {
        //ConfigurationManager.AppSettings["VersionAplication"].ToString()
        #region "Singleton"

        // Solo permitimos una instacia unica del modelo
        // el acceso debe realizarse a traves de la propiedad Instance

        static DataAccessSQL classInstance = null;
        static object classLock = new object();

        public static DataAccessSQL Instance
        {
            get
            {
                lock (classLock)
                {
                    if (classInstance == null)
                    {
                        classInstance = new DataAccessSQL();
                    }
                }
                return classInstance;
            }
        }


        #endregion


        #region "Variables"

        DbClient dbClient { get; set; }
        StoredProcedures procedures { get; set; }
        #endregion


        #region "Constructor"

        public DataAccessSQL()
        {
            procedures = new StoredProcedures();
        }

        #endregion

        public Library.DbClient.IDbCommand SelectedCommand(string store, string ConnectionString)
        {
            string connection = ConnectionString; //StoredProcedures.StoresDataBase[store];
            DbConnectionString cn = new DbConnectionString("MainConnectionString", connection);
            this.dbClient = new DbClient(cn);
            this.dbClient.AddCommand(store);
            return this.dbClient.GetCommand(store);
        }

        public ObservableCollection<GeneralPiece> GetProductionGeneral(Dictionary<String, object> listParams, string ConnectionString)
        {
            Library.DbClient.IDbCommand cm;

            try
            {
                if (Configurations.Instance.Machine.Equals("Forjadora 0"))
                {
                    cm = SelectedCommand(StoredProcedures.GetProductionGeneralV2, ConnectionString);
                }                       //*TO-DO*// Verificar validacion secuencia
                else if (Configurations.Instance.Opcion == "Mecanizado 2")
                {
                    cm = SelectedCommand(StoredProcedures.GetProductionGeneralProgrammedTorno2, ConnectionString);
                }
                else if (Configurations.Instance.Opcion == "Mecanizado 4")
                {
                    cm = SelectedCommand(StoredProcedures.GetProductionGeneralProgrammedTorno4, ConnectionString);
                }
                else if ((Convert.ToInt16(Configurations.Instance.Secuencia) > 7 || Configurations.Instance.VersionApplication.Equals("V3")) && Convert.ToInt16(Configurations.Instance.Secuencia) < 11)
                {

                    cm = SelectedCommand(StoredProcedures.GetProductionGeneralProgrammed, ConnectionString);
                }
                else
                {
                    cm = SelectedCommand(StoredProcedures.GetProductionGeneral, ConnectionString);
                }


                ObservableCollection<GeneralPiece> result = new ObservableCollection<GeneralPiece>();


                //cm.Timeout = 50000;


                using (var dr = cm.ExecuteReader(listParams.ToReadOnlyDictionary()))
                {
                    Trace.Message("listParams: {0},{1},{2}", listParams["@Orden"], listParams["@Colada"], listParams["@Atado"]);
                    GeneralPiece row;

                    while (dr.Read())
                    {




                        row = new GeneralPiece()
                        {
                            IdHistory = Convert.ToInt32(dr["IdHistory"]),
                            OrderNumber = Convert.ToInt32(dr["OrderNumber"]),
                            HeatNumber = dr.GetSchemaTable().Select("ColumnName='HeatNumber'").Count() == 1 ? Convert.ToInt32(dr["HeatNumber"]) : 0,
                            Customer = dr["Customer"].ToString(),
                            GroupItemNumber = dr.GetSchemaTable().Select("ColumnName='GroupItemNumber'").Count() == 1 ? Convert.ToInt32(dr["GroupItemNumber"]) : 0,
                            LotNumberHTR = dr.GetSchemaTable().Select("ColumnName='LotNumberHTR'").Count() == 1 ? Convert.ToInt32(dr["LotNumberHTR"]) : 0,
                            LoadedCount = dr.GetSchemaTable().Select("ColumnName='LoadedCount'").Count() == 1 ? Convert.ToInt32(dr["LoadedCount"]) : 0,
                            GoodCount = dr.GetSchemaTable().Select("ColumnName='GoodCount'").Count() == 1 ? Convert.ToInt32(dr["GoodCount"]) : 0,
                            ScrapCount = dr.GetSchemaTable().Select("ColumnName='ScrapCount'").Count() == 1 ? Convert.ToInt32(dr["ScrapCount"]) : 0,
                            WarnedCount = dr.GetSchemaTable().Select("ColumnName='WarnedCount'").Count() == 1 ? Convert.ToInt32(dr["WarnedCount"]) : 0,
                            ReworkedCount = dr.GetSchemaTable().Select("ColumnName='ReworkedCount'").Count() == 1 ? Convert.ToInt32(dr["ReworkedCount"]) : 0,
                            PendingCount = dr.GetSchemaTable().Select("ColumnName='PendingCount'").Count() == 1 ? Convert.ToInt32(dr["PendingCount"]) : 0,
                            Description = dr["Description"].ToString(),
                            Location = dr["Location"].ToString(),
                            IdBatch = dr.GetSchemaTable().Select("ColumnName='IdBatch'").Count() == 1 ? Convert.ToInt32(dr["IdBatch"]) : 0,
                            InsDateTime = DateTime.Parse(dr["InsDateTime"].ToString()),
                            Extremo = dr["Extremo"].ToString(),
                            ReportBox = dr.GetSchemaTable().Select("ColumnName='ReportBox'").Count() == 1 ? dr["ReportBox"].ToString() : null,
                            GroupItemType = dr.GetSchemaTable().Select("ColumnName='GroupItemType'").Count() == 1 ? dr["GroupItemType"].ToString() : null,

                            ShiftDate = dr.GetSchemaTable().Select("ColumnName='ShiftDate'").Count() == 1 ? dr["ShiftDate"].ToString() : null,
                            ShiftNumber = dr.GetSchemaTable().Select("ColumnName='ShiftNumber'").Count() == 1 ? dr["ShiftNumber"].ToString() : null

                            //No existen en version 1

                            //ReportSequence = dr.GetSchemaTable().Select("ColumnName='ReportSequence'").Count() == 1 ? Convert.ToInt16(dr["ReportSequence"]) : 0 // NO SE ENCUENTRA EN NINGUNA VERSION
                            //SendStatus = (Enums.Enumerations.ProductionReportSendStatus)Convert.ToInt32(dr["SendStatus"]),
                            //Sended = (Enums.Enumerations.AxlrBit)Convert.ToInt32(dr["Sended"]),
                            //


                        };

                        result.Add(row);




                    }
                }



                return result;
                
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return null;

            }
            finally
            {
                this.dbClient.Dispose();
            }
        }




        internal string GetCurrentUser()
        {

            var cm = SelectedCommand(StoredProcedures.GetCurrentUserByWorkstation, Configurations.Instance.ConnectionString);
            Dictionary<string, object> listParams = new Dictionary<string, object>();
            listParams.Add("@Workstation", Configurations.Instance.Workstation);

            string result = "";
            try
            {
                using (var dr = cm.ExecuteReader(listParams.ToReadOnlyDictionary()))
                {
                    if (dr.Read())
                    {
                        result = (string)dr["Identification"];

                    }
                    return result;
                }
            }
            catch
            {
                return result;
            }
        }

        public ObservableCollection<ReportProductionHistory> GetReportProductionHistory(Dictionary<String, object> listParams, string ConnectionString)
        {

            try
            {

                var cm = SelectedCommand(StoredProcedures.GetReportProductionHistory, ConnectionString);
                Trace.Message("DB Command to execute: {0} ||| Connection String: {1}", StoredProcedures.GetReportProductionHistory, ConnectionString);
                ObservableCollection<ReportProductionHistory> result = new ObservableCollection<ReportProductionHistory>();

                using (var dr = cm.ExecuteReader(listParams.ToReadOnlyDictionary()))
                {
                    ReportProductionHistory row;
                    Trace.Message("DBCommand executed. Reading starting...");
                    int i = 0;
                    while (dr.Read())
                    {
                        row = new ReportProductionHistory()
                        {
                            Id = Convert.ToInt32(dr["IdReportProductionHistory"]),
                            IdHistory = Convert.ToInt32(dr["IdHistory"]),
                            IdOrder = Convert.ToInt32(dr["OrderNumber"]),
                            HeatNumber = Convert.ToInt32(dr["HeatNumber"]),
                            GroupItemNumber = Convert.ToInt32(dr["GroupItemNumber"]),
                            SendStatus = (Enums.Enumerations.ProductionReportSendStatus)Convert.ToInt32(dr["ScrapCount"]),
                            TotalQuantity = Convert.ToInt32(dr["TotalQuantity"]),
                            GoodCount = Convert.ToInt32(dr["GoodCount"]),
                            ScrapCount = Convert.ToInt32(dr["ScrapCount"]),
                            ReworkedCount = Convert.ToInt32(dr["ReworkedCount"]),
                            LotNumberHtr = Convert.ToInt32(dr["LotNumberHtr"]),
                            InsDateTime = Convert.ToDateTime(dr["InsDateTime"].ToString()),
                            IdMachine = Convert.ToInt32(dr["IdMachine"]),
                            InsertedBy = dr["InsertedBy"].ToString(),
                            MachineSequence = Convert.ToInt32(dr["MachineSequence"]),
                            MachineOption = dr["MachineOption"].ToString(),
                            MachineOperation = dr["MachineOperation"].ToString(),
                            Observation = dr["Observation"].ToString(),


                        };

                        result.Add(row);
                        i++;
                    }
                    Trace.Message("Reading complete, number of results: {0}", i);
                }

                return result;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                this.dbClient.Dispose();
                Trace.Message("dbClient disposed");
            }
        }

        public int IsBoxSelect(int numberOrderMotehr, string idBox)
        {


            int resul = 0;
            try
            {

                var command = SelectedCommand(StoredProcedures.IsBoxSelect, Configurations.Instance.ConnectionString);
                Trace.Message("DBCommand to use: {0} ||| Connection string: {1}", StoredProcedures.IsBoxSelect, Configurations.Instance.ConnectionString);
                Dictionary<string, object> listParams = new Dictionary<string, object>();
                listParams.Add("@IdBox", idBox);
                listParams.Add("@NumerOrderMother", numberOrderMotehr);

                BoxReport box;
                var dr = command.ExecuteReader(listParams.ToReadOnlyDictionary());
                string result = "";
                while (dr.Read())
                {
                    result = (string)dr["IdBox"];
                }

                result = string.IsNullOrEmpty(result) ? "" : result;
                Trace.Message("DBCommand executed succesfuly");

                resul = result.Equals(idBox, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0;

            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
            }

            return resul;
        }



        public int GetActiveBox()
        {
            var command = SelectedCommand(StoredProcedures.GetActiveBox, Configurations.Instance.ConnectionString);

            var idBox = command.ExecuteScalar();

            return Convert.ToInt32(idBox);
        }


        public string BoxSelect(int numberOrderMotehr)
        {
            string resul = string.Empty;
            try
            {


                var command = SelectedCommand(StoredProcedures.IsBoxSelect, Configurations.Instance.ConnectionString);
                Trace.Message("Selected Command: {0} ||| Connection string: {1}", StoredProcedures.IsBoxSelect, Configurations.Instance.ConnectionString);
                //var machineConfig = ConfigurationManager.AppSettings["Machine"].ToString();
                //command.Parameters["@IdBox"].Value = idBox;
                command.Parameters["@NumerOrderMother"].Value = numberOrderMotehr;
                //command.Parameters["@Resul"].Value = 0;
                command.Parameters["@Resul"].Direction = ParameterDirection.Output;

                Trace.Message("Parameters values: (NumerOrderMother = {0}) (@Resul = {1})", numberOrderMotehr, ParameterDirection.Output);
                command.ExecuteTable();


                resul = command.Parameters["@Resul"].Value.ToString();

            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
            }

            return resul;
        }

        public void UpdBoxReportada(string idBox)
        {
            try
            {
                var command = SelectedCommand(StoredProcedures.UpdBoxReportada, Configurations.Instance.ConnectionString);
                Trace.Message("DataAccesSQL.UpdBoxReportada(idBox={0}) || SelectedCommand: {1} || ConnectionString: {2}", idBox, StoredProcedures.UpdBoxReportada, Configurations.Instance.ConnectionString);
                command.Parameters["@IdBox"].Value = idBox;
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
            }
        }

        public int GetPreviousSequence(string description)
        {
            try
            {


                var command = SelectedCommand(StoredProcedures.GetPreviousSequence, Configurations.Instance.ConnectionString);
                Trace.Message("DataAccessSQL.GetPreviousSequence(description {0}) || SelectedCommand: {1} || ConnectionString: {2}", description, StoredProcedures.GetPreviousSequence, Configurations.Instance.ConnectionString);
                command.Parameters["@Description"].Value = description;

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return 8;
            }
        }

        public int GetPreviousSequenceByOperation(string operation)
        {
            try
            {
                var command = SelectedCommand(StoredProcedures.GetPreviousSequenceByOperation, Configurations.Instance.ConnectionString);
                Trace.Message("DataAccessSQL.GetPreviousSequenceByOperation(operation= {0}) || SelectedCommand: {1} || ConnectionString: {2}", operation, StoredProcedures.GetPreviousSequenceByOperation, Configurations.Instance.ConnectionString);
                command.Parameters["@MachineOperation"].Value = operation.Trim();

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return 0;
            }
        }


        public ObservableCollection<BoxReport> GetBoxesForPainting(int udtBox)
        {
            try
            {
                var cm = SelectedCommand(StoredProcedures.GetBoxesForPainting, Configurations.Instance.ConnectionString);
                Trace.Message("DataAccessSQL.GetBoxesForPainting(udtBox= {0}) || SelectedCommand: {1} || ConnectionString: {2}", udtBox, StoredProcedures.GetBoxesForPainting, Configurations.Instance.ConnectionString);
                Dictionary<string, object> listParams = new Dictionary<string, object>
                {
                    { "@UdtBox", udtBox }
                };
                ObservableCollection<BoxReport> result = new ObservableCollection<BoxReport>();
                using (var dr = cm.ExecuteReader(listParams.ToReadOnlyDictionary()))
                {
                    BoxReport row;
                    Trace.Message("(DataAccessSQL.GetBoxesForPainting): ExecuteReader completed");
                    while (dr.Read())
                    {
                        row = new BoxReport()
                        {
                            Caja = dr["Caja"].ToString(),
                            Colada = dr["Colada"].ToString(),
                            OrdenHija = dr["OrdenHija"].ToString(),
                            OrdenOrigen = dr["OrdenOrigen"].ToString(),
                            TipoUdt = dr["TipoUDT"].ToString(),
                            MaquinaAnterior = dr["MaquinaAnterior"].ToString(),
                            MachineOperation = dr["MachineOperation"].ToString(),
                            CantidadTotal = dr["CantidadTotal"].ToString(),
                            PiezasBuenas = dr["PiezasBuenas"].ToString(),
                            PiezasMalas = dr["PiezasMalas"].ToString(),
                            IdMachine = dr["idMachine"].ToString(),

                        };

                        result.Add(row);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return null;
            }
        }


        public int InsLoadPintado(PaintingReport reportProductionDto)
        {
            try
            {


                var command = SelectedCommand(StoredProcedures.InsLoadPintado, Configurations.Instance.ConnectionString);
                Trace.Message("DataAccessSQL.InsLoadPintado(reportProductionDto.HeatNumberCode= {0}) || SelectedCommand: {1} || ConnectionString: {2}", reportProductionDto.HeatNumberCode, StoredProcedures.InsLoadPintado, Configurations.Instance.ConnectionString);
                command.Parameters["@BoxUdt"].Value = reportProductionDto.BoxUdt;
                command.Parameters["@ParentUdt"].Value = reportProductionDto.ParentUdt;
                command.Parameters["@ChildOrder"].Value = reportProductionDto.ChildOrden;
                command.Parameters["@ParentOrder"].Value = reportProductionDto.ParentOrden;
                command.Parameters["@HeatNumber"].Value = reportProductionDto.HeatNumber;
                command.Parameters["@HeatNumberCode"].Value = reportProductionDto.HeatNumberCode;
                command.Parameters["@LoadQuantity"].Value = reportProductionDto.LoadQuantity;
                command.Parameters["@SendedQuantity"].Value = reportProductionDto.SendedQuantiry;
                command.Parameters["@Storage"].Value = reportProductionDto.Storage;
                command.Parameters["@NextSequence"].Value = reportProductionDto.NextSequence;
                command.Parameters["@NextOperation"].Value = reportProductionDto.NextOperation;
                command.Parameters["@NextOption"].Value = reportProductionDto.NextOption;
                command.Parameters["@LotId"].Value = reportProductionDto.LotId;
                command.Parameters["@UdtType"].Value = reportProductionDto.UdtType;
                command.Parameters["@UdcType"].Value = reportProductionDto.UdcType;

                return Convert.ToInt32(command.ExecuteScalar());
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return 0;
            }
        }


        public ObservableCollection<BoxLoad> GetLoadPainting(int cajon)
        {
            try
            {
                var cm = SelectedCommand(StoredProcedures.GetLoadPainting, Configurations.Instance.ConnectionString);
                Dictionary<string, object> listParams = new Dictionary<string, object>
                {
                    { "@ChildOrder", null },
                    { "@HeatNumber", null },
                    { "@BoxUdt", cajon }
                };
                Trace.Message("DataAccessSQL.GetLoadPainting(listparams= {0}) || SelectedCommand: {1} || ConnectionString: {2}", 
                    listParams["@ChildOrder"] + " " + listParams["@HeatNumber"] + " " + listParams["@BoxUdt"], 
                    StoredProcedures.GetLoadPainting, Configurations.Instance.ConnectionString);

                ObservableCollection<BoxLoad> result = new ObservableCollection<BoxLoad>();

                using (var dr = cm.ExecuteReader(listParams.ToReadOnlyDictionary()))
                {
                    BoxLoad row;
                    Trace.Message("(DataAccessSQL.GetLoadPainting): ExecuteReader Completed");
                    while (dr.Read())
                    {
                        row = new BoxLoad()
                        {
                            IdLoadPainting = dr["idLoadPainting"].ToString(),
                            Order = dr["Order"].ToString(),
                            Colada = dr["Colada"].ToString(),
                            CodigoColada = dr["Codigo Colada"].ToString(),
                            TipoUdt = dr["TipoUdt"].ToString(),
                            IdUdt = dr["IdUdt"].ToString(),
                            TipoUdc = dr["TipoUdc"].ToString(),
                            Lote = dr["Lote"].ToString(),
                            Cantidad = dr["Cantidad"].ToString(),
                            Almacen = dr["Almacen"].ToString(),
                            Extremo = dr["Extremo"].ToString(),
                            SecuenciaSiguiente = dr["SecuenciaSiguiente"].ToString(),
                            OperacionSiguiente = dr["OperacionSiguiente"].ToString(),
                            OpcionSiguiente = dr["OpcionSiguiente"].ToString(),
                            Lot4 = dr["Lot4"].ToString(),
                            LotId = dr["LotId"].ToString(),
                            ProductReportBox = dr["ProductReportBox"].ToString(),
                            Active = dr["Active"].ToString()


                        };

                        result.Add(row);
                    }
                }

                return result;


                //command.Parameters["@ChildOrder"].Value = childOrder;
                //command.Parameters["@HeatNumber"].Value = heatNumber;
                //command.Parameters["@BoxUdt"].Value = string.IsNullOrEmpty(boxUdt) ? null : boxUdt;

                //return command.ExecuteTable();
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
                return null;
            }
        }


        public OPChildrens GetNextOpChildrenActive(int OrdenNumberMother)
        {
            OPChildrens opHija = null;
            var cm = SelectedCommand(StoredProcedures.GetNextOpChildrenActive, Configurations.Instance.ConnectionString);
            Trace.Message("DataAccessSQL.GetNextOpChildrenActive(OrdenNumberMother= {0}) || SelectedCommand: {1} || ConnectionString: {2}", OrdenNumberMother, StoredProcedures.GetNextOpChildrenActive, Configurations.Instance.ConnectionString);
            try
            {
                //CreateDbClient();
                //var command = _dbClient.GetCommand(Get("GetNextOpChildrenActive"));
                //var machineConfig = ConfigurationManager.AppSettings["Machine"].ToString();
                Dictionary<string, object> Param = new Dictionary<string, object>();
                Param.Add("@ordernumber", OrdenNumberMother);

                DataTable resul = cm.ExecuteTable(Param.ToReadOnlyDictionary());// .ExecuteReader();
                if (resul.Rows.Count > 0)
                {
                    opHija = new OPChildrens();
                    foreach (DataRow row in resul.Rows)
                    {
                        opHija.NumeroOrder = Convert.ToInt32(row["OrderNumber"].ToString());
                        opHija.Cople = row["Cople"].ToString();
                        opHija.Centralizado = row["Centralizado"].ToString();
                        opHija.Cabezal = row["Cabezal"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.Exception(ex);
            }

            return opHija;
        }
    }
}


