﻿using log4net;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using Microsoft.Practices.Prism.ViewModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using Tenaris.Fava.Production.Reporting.Model.Adapter;
using Tenaris.Fava.Production.Reporting.Model.Business;
using Tenaris.Fava.Production.Reporting.Model.DTO;
using Tenaris.Fava.Production.Reporting.Model.Enums;
using Tenaris.Fava.Production.Reporting.Model.Model;
using Tenaris.Fava.Production.Reporting.Model.Support;
using Tenaris.Fava.Production.Reporting.ViewModel.Dialog;
using Tenaris.Library.Log;

namespace Tenaris.Fava.Production.Reporting.ViewModel.Support
{
    public class ProductionReportSupport : NotificationObject
    {
        #region
        static GeneralPiece currentGeneralPieceDto;
        static ReportProductionDto currentProductionReportDto;
        static ReportProductionDto productionReportDto;
        static IList rejectionReportDetails = new List<RejectionReportDetail>();
        static private string user;
        static int tbScrapCountL2;
        static int tbReworkedCountL2;
        static int tbLoadedCountL2;
        static int tbGoodCountL2;
        static bool cancel;
        private const string ext1 = "Extremo 1";
        private const string ext2 = "Extremo 2";

        // private static ISecurityAdapter Adapter { get; set; }

        protected static IDictionary<string, string> adaptersInformation;

        public static bool canReportOnConfirm = false;
        public static string whoIsLogged = "";
        //V1: OplSubscription oplSubscription;
        public static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static IList<GeneralPiece> currentGeneraPieces = null;

        public static ReportConfirmationBusiness ReportConfirmationBusiness { get; set; }
        public static void ProductionReport_Load()
        {
            log4net.Config.XmlConfigurator.Configure();
            System.Runtime.Remoting.RemotingConfiguration.Configure(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, false);
            log.Debug("Starting...");
            //PopulateGeneralProduction(txtOrden.Text, txtColada.Text, txtAtado.Text);      
            //V1: oplSubscription =new OplSubscription();
            //V1: oplSubscription.OpenSession(); 
        }

        //** Listo **//
        public static ObservableCollection<GeneralPiece> PopulateGeneralProduction(int Orden, int Colada, int Atado, string ConnectionString)
        {
            Trace.Message("Starting PopulateGeneralProduction Order:{0}, Colada:{1}, Atado:{2}, connectionstring {3}", Orden, Colada, Atado, ConnectionString);



            var generalPieces = ProductionReport.GetProductionGeneral(Orden, Colada, Atado, Configurations.Instance.ConnectionString);

            generalPieces = ProductionReport.GetSendStatusForGeneralPieces(generalPieces);
            Trace.Message("generalpieces {0}", generalPieces);
            if (generalPieces != null)
            {
                if (!String.IsNullOrEmpty(Configurations.Instance.MachineFiltre))
                {
                    currentGeneraPieces = ProductionReport.ClassifyBySendStatus(generalPieces, ConnectionString).Where(x => x.Description == Configurations.Instance.MachineFiltre).ToList();
                    Trace.Message("Configurations.Instance.MachineFiltre != null {0}--currentGeneraPieces {1}", Configurations.Instance.MachineFiltre, currentGeneraPieces);

                }
                else
                {
                    currentGeneraPieces = ProductionReport.ClassifyBySendStatus(generalPieces, ConnectionString).Where(x => x.Description == Configurations.Instance.Machine).ToList();
                    Trace.Message("Configurations.Instance.MachineFiltre = null {0}--currentGeneraPieces {1}", Configurations.Instance.Machine, currentGeneraPieces);


                }
                if (Configurations.Instance.Machine.Contains("Roscadora"))
                {
                    currentGeneraPieces = currentGeneraPieces.Where(x => (x.SendStatus == Enumerations.ProductionReportSendStatus.Parcial) || (x.SendStatus == Enumerations.ProductionReportSendStatus.Completo)).ToList();
                    Trace.Message("Configurations.Instance.Machine = Roscadora {0}--currentGeneraPieces {1}", Configurations.Instance.Machine, currentGeneraPieces);

                }
                else if ((Configurations.Instance.Machine == "Forjadora")) //Checar el modo en que está operando la forjadora 1. BothEnds or OneEnd
                {
                    currentGeneraPieces = GetForgeCurrentGeneralPieces(currentGeneraPieces, Atado);
                    Trace.Message("Configurations.Instance.Machine = Forjadora {0}--currentGeneraPieces {1}", Configurations.Instance.Machine, currentGeneraPieces);

                }
            }
            //if (ConfigurationManager.AppSettings["Extremo"].ToString() != "")
            //    currentGeneraPieces = currentGeneraPieces.Where(x => x.Extremo == ConfigurationManager.AppSettings["Extremo"].ToString()).ToList();


            if (Configurations.Instance.Machine == "Forjadora 0")
            {
                ObservableCollection<GeneralPiece> currentGeneralPiecesForja0 = new ObservableCollection<GeneralPiece>();
                foreach (GeneralPiece cgeneralPiece in currentGeneraPieces)
                {

                    currentGeneralPiecesForja0.Add(dgReporteProduccion_SelectionChangedForja0(cgeneralPiece));

                }
                currentGeneraPieces = currentGeneralPiecesForja0;
                return new ObservableCollection<GeneralPiece>(currentGeneraPieces);
            }





            return new ObservableCollection<GeneralPiece>(currentGeneraPieces);


        }

        public static IList<GeneralPiece> GetForgeCurrentGeneralPieces(IList<GeneralPiece> currentPieces, int Atado)
        {

            var forgeMode = new ProductionReportFacade().GetCurrentForgeMode(Atado);


            if (forgeMode == Enumerations.ForgeMode.OneEnd)
            {
                currentPieces = currentPieces.Where(x => x.Extremo == "Extremo 1").ToList();
                foreach (var currentPiece in currentPieces)
                {
                    var productionReport = new ProductionReport();
                    currentPiece.Extremo = "Extremo 2";
                    //if(productionReport.IsForjadoraAndForgeModeIsOnline(currentPiece.GroupItemNumber)) 
                    currentPiece.LoadedCount = new ReportProductionHistoryFacade().
                     GetLastMachineGoodPieces(currentPiece.OrderNumber,
                       currentPiece.HeatNumber, currentPiece.GroupItemNumber,
                       currentPiece.Description, currentPiece.Extremo);
                }
            }

            return currentPieces;
        }




        public static GeneralPiece GetCurrentGeneralPiece(int idHistory)
        {

            var filteredList = currentGeneraPieces.Where(c => c.IdHistory == idHistory).ToList<GeneralPiece>();

            if (filteredList.Count > 0)
                return currentGeneraPieces.Where(c => c.IdHistory == idHistory).First();
            else
                return currentGeneralPieceDto;//null;
        }

        // ---------------------------------------------------------------------------------------------------------

        public static int GetFirstPieceLoadedNumberForIT(GeneralPiece GeneralPiece)
        {
            // PRUEBAS BGR (Se puso la validación para las máquinas)

            if (GeneralPiece.Description == "Forjadora 0")
            {
                var a = currentGeneraPieces.Where(c => (c.OrderNumber == GeneralPiece.OrderNumber)
                 && (c.GroupItemNumber == GeneralPiece.GroupItemNumber)
                 && (c.HeatNumber == GeneralPiece.HeatNumber)
                 && (c.ReportSequence == 1) && (c.Extremo == GeneralPiece.Extremo)).First();
                return a.PendientesPorReportar + a.TotalReportado;
            }
            else
            {
                return currentGeneraPieces.Where(c => (c.OrderNumber == GeneralPiece.OrderNumber)
                && (c.GroupItemNumber == GeneralPiece.GroupItemNumber)
                && (c.HeatNumber == GeneralPiece.HeatNumber)
                && (c.ReportSequence == 1) && (c.Extremo == GeneralPiece.Extremo)).First().LoadedCount;
            }

        }

        // ---------------------------------------------------------------------------------------------------------

        public static bool Report(GeneralPiece currentDGRow, InteractionRequest<Notification> request, InteractionRequest<Notification> IndBoxReportConfirmationRequest,
            InteractionRequest<Notification> showErrorMessageRequest, InteractionRequest<Notification> showMessageRequest,
            InteractionRequest<Notification> showQuestionRequest, ObservableCollection<ReportProductionHistory> historico)
        {

            //Verifica que el extremo 1 o 2 no hayan sido enviados
            //if(Configurations.Instance.Machine.Contains("Forjadora"))
            //{

            //    if (currentDGRow.Extremo == "Extremo 1" && historico.Where(x => x.MachineOperation.Contains("1") && (x.SendStatus == Enumerations.ProductionReportSendStatus.Completo || x.SendStatus == Enumerations.ProductionReportSendStatus.Final)).ToList().Count > 0)
            //    {
            //        ShowError showError = new ShowError("Error", string.Format("Este reporte ya ha sido enviado como EXTREMO 1. Operación cancelada"));
            //        showErrorMessageRequest.Raise(new Notification() { Content = showError });
            //        return false;
            //    }
            //    if (currentDGRow.Extremo == "Extremo 2" && historico.Where(x => x.MachineOperation.Contains("2") && (x.SendStatus == Enumerations.ProductionReportSendStatus.Completo || x.SendStatus == Enumerations.ProductionReportSendStatus.Final)).ToList().Count > 0)
            //    {
            //        ShowError showError = new ShowError("Error", string.Format("Este reporte ya ha sido enviado como EXTREMO 2. Operación cancelada"));
            //        showErrorMessageRequest.Raise(new Notification() { Content = showError });
            //        return false;
            //    }
            //}


            bool response;
            if (Configurations.Instance.VersionApplication.Equals("V1") && !Configurations.Instance.Secuencia.Equals("8"))
            {
                Trace.Message("ReportV1 currentDgRow {0}", currentDGRow);
                response = ReportV1(currentDGRow, request, showErrorMessageRequest, showMessageRequest, showQuestionRequest);
                //ShowMessage showMessage = new ShowMessage("Reporte de Producción", "La UDT declarada en el servicio\n(Tarjeta de Linea 14024968 colada 321697) actualmente\n no se encuentra cargada en el puesto seleccionado (3-Control \n No Destructivo-Control No Destructivo)");
                //showMessageRequest.Raise(new Notification() { Content = showMessage });
            }

            else
            {
                Trace.Message("ReportV3 currentDgRow {0}", currentDGRow);
                response = ReportV3(currentDGRow, request, showErrorMessageRequest, showMessageRequest, showQuestionRequest, IndBoxReportConfirmationRequest);
            }
            if (response)
            {
                return false;
            }

            return false;
        }

        private static bool ReportExecute(InteractionRequest<Notification> showErrorMessageRequest, InteractionRequest<Notification> showMessageRequest,
            InteractionRequest<Notification> showQuestionRequest,
            ReportConfirmationViewModel reportConfirmation = null,
            IndBoxReportConfirmationViewModel boxReportConfirmation = null)
        {

            if (boxReportConfirmation == null)
            {
                currentProductionReportDto = reportConfirmation.CurrentReportProduction;
                currentProductionReportDto.IdUDT = reportConfirmation.Atado;
                currentProductionReportDto.Orden = reportConfirmation.Orden;
                currentProductionReportDto.Colada = reportConfirmation.Colada;
                string SelectedSendType = reportConfirmation.SelectedSendType;
                ObservableCollection<RejectionReportDetail> dgRejectionReportDetails = reportConfirmation.RejectionReportDetails;
                int lbITLoadHelper = reportConfirmation.ITLoadHelper;
                int tbTotalLoaded = reportConfirmation.CargadasTotal;
                int tbPreviousLoaded = reportConfirmation.CargadasAnterior;
                string _User = whoIsLogged;

                tbScrapCountL2 = reportConfirmation.MalasActual;
                tbReworkedCountL2 = reportConfirmation.ReprocesosActual;
                tbLoadedCountL2 = reportConfirmation.CargadasActual;
                tbGoodCountL2 = reportConfirmation.BuenasActual;


                bool confirmation = ReportConfirmationSupport.ReportConfirmation(tbGoodCountL2, tbScrapCountL2, tbReworkedCountL2, tbLoadedCountL2, reportConfirmation.Extremo, 5,
                    reportConfirmation.RejectionReportDetails, dgRejectionReportDetails, currentProductionReportDto, lbITLoadHelper, tbTotalLoaded,
                    SelectedSendType, tbPreviousLoaded, _User, showQuestionRequest, showMessageRequest, showErrorMessageRequest);
                #region codigo redundante
                //if (confirmation)
                //{
                //    ReportConfirmationSupport.GetSendStatus(SelectedSendType);
                //    {
                //        if (ValidationRules.ValidateRejectionReasons(tbScrapCountL2,
                //            rejectionReportDetails))
                //        {
                //            var confirmMessage = string.Format("Resumen de lo Reportado: \n\n Buenas:{0} \n" +
                //                " Malas:{1} \n Reprocesos:{2} \n Total:{3} \n\n ¿Desea reportar estas cantidades?", tbGoodCountL2,
                //                tbScrapCountL2, tbReworkedCountL2, tbLoadedCountL2);

                //            ShowQuestion showQuestion = new ShowQuestion("Confirmar Reporte", confirmMessage);
                //            showQuestionRequest.Raise(new Notification() { Content = showQuestion });
                //            if (showQuestion.Result)
                //            {
                //                //if (chBEdit.Checked)
                //                ReportConfirmationSupport.PrepareDtoForProductionReport(currentProductionReportDto, lbITLoadHelper, tbPreviousLoaded,
                //    tbLoadedCountL2, tbGoodCountL2, tbScrapCountL2, tbReworkedCountL2, user);
                //                int versionInt = Convert.ToInt32(Configurations.Instance.VersionApplication.Replace("V", ""));
                //                var response = new ITServiceAdapter().ReportProduction(
                //                    user, currentProductionReportDto, ReportConfirmationSupport.GetSendStatus(SelectedSendType),
                //                        true, rejectionReportDetails, versionInt);

                //                ShowMessage showMessage = new ShowMessage("Reporte de Producción", response);
                //                showMessageRequest.Raise(new Notification() { Content= showMessage });
                //                ReportConfirmationSupport.CheckReportProductionForNextOperation(response, currentProductionReportDto, lbITLoadHelper, tbTotalLoaded, showMessageRequest);

                //            }

                //        }
                //    }
                //    return true;
                //}
                #endregion
                return confirmation;
            }
            else
            {
                //currentProductionReportDto = reportConfirmation.CurrentReportProduction;
                //currentProductionReportDto.IdUDT = reportConfirmation.Atado;
                //currentProductionReportDto.Orden = reportConfirmation.Orden;
                //currentProductionReportDto.Colada = reportConfirmation.Colada;
                //string SelectedSendType = reportConfirmation.SelectedSendType;
                //ObservableCollection<RejectionReportDetail> dgRejectionReportDetails = reportConfirmation.RejectionReportDetails;
                //int lbITLoadHelper = reportConfirmation.ITLoadHelper;
                //int tbTotalLoaded = reportConfirmation.CargadasTotal;
                //int tbPreviousLoaded = reportConfirmation.CargadasAnterior;
                string _User = whoIsLogged;

                ProductionBox selectedBox = boxReportConfirmation.SelectedBox;


                tbScrapCountL2 = boxReportConfirmation.Malas;
                tbReworkedCountL2 = boxReportConfirmation.Reprocesos;
                tbLoadedCountL2 = boxReportConfirmation.Total;
                tbGoodCountL2 = boxReportConfirmation.Buenas;

                bool confirmation = IndBoxReportConfirmationSupport.BoxReportConfirmation(tbGoodCountL2, tbReworkedCountL2, boxReportConfirmation.Total, boxReportConfirmation.TotalActualAtado, tbScrapCountL2,
                    selectedBox, boxReportConfirmation.currentGeneralPiece, boxReportConfirmation.currentProductionReport, string.IsNullOrEmpty(boxReportConfirmation.OpHija) ? 0 : Convert.ToInt32(boxReportConfirmation.OpHija), boxReportConfirmation.ChangeReason,
                    boxReportConfirmation.DgBoxes, boxReportConfirmation.User, showQuestionRequest, showMessageRequest, showErrorMessageRequest);
                return confirmation;
            }
        }

        private static bool ReportV1(GeneralPiece currentDGRow, InteractionRequest<Notification> request,
            InteractionRequest<Notification> showErrorMessageRequest, InteractionRequest<Notification> showMessageRequest,
            InteractionRequest<Notification> showQuestionRequest)
        {

            bool response = true;
            //OplSubscription oplSubscription = new OplSubscription();
            //oplSubscription.OpenSession();
            if (Login())
            {
                //PRUEBADWF
                #region FilasHardcode
                //GeneralPiece prueba1 = new GeneralPiece()
                //{
                //    GroupItemNumber = 12345678,
                //    BuenasReportadas = 0,
                //    Cargados = 0,
                //    Customer = "AAA",
                //    Description = "FORJADORA 0",
                //    Extremo = "Extremo 1",
                //    Sended = Model.Enums.Enumerations.AxlrBit.No,
                //    SendStatus = Model.Enums.Enumerations.ProductionReportSendStatus.Parcial,
                //    SendedString = "No",
                //    LoadedCount = 10,
                //    ReworkedCount = 20,
                //    GoodCount = 30,
                //    ScrapCount = 40,
                //    LotNumberHTR = 50,
                //    HeatNumber = 12345,
                //    Location = "AQUI",
                //    OrderNumber = 0001,
                //    IdHistory = 11,
                //    IdBatch = 23,
                //    GroupItemType = "nose",
                //    ReportSequence = 1



                //};
                //currentGeneraPieces.Add(prueba1);
                //acabaPRUEBA
                #endregion

                var reportProductionDto = GetCurrentGroupItemToReport(currentDGRow);
                if (reportProductionDto != null)
                {

                    bool bypass = ConfigurationManager.AppSettings["Bypass"] == "true" ? true : false;
                    if (reportProductionDto.Enviado == Enumerations.AxlrBit.No || bypass || Configurations.Instance.Machine.Contains("Forjadora"))
                    {
                        var generalPieceDto = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                        if (ValidationRules.ValidateReportSequence(currentGeneraPieces, generalPieceDto))
                        {

                            var maquina = Configurations.Instance.Machine.ToUpper();
                            if (maquina == reportProductionDto.DescripcionMaquina.ToUpper())
                            {
                                string user = whoIsLogged;
                                ReportConfirmationViewModel reportConfirmation = new ReportConfirmationViewModel(
                                    generalPieceDto, reportProductionDto, GetFirstPieceLoadedNumberForIT(generalPieceDto), new OplSubscription(), user);

                                Trace.Message("reportConfirmation:GoodCount:{0},ScrapCount:{1},LoadedCount:{2}---> user:{3}", generalPieceDto.GoodCount, generalPieceDto.ScrapCount, generalPieceDto.LoadedCount, user);
                                request.Raise(new Notification() { Content = reportConfirmation });


                                if (reportConfirmation.Result)
                                {
                                    if (Configurations.Instance.Machine.Contains("Forjadora") || Configurations.Instance.Machine.Contains("Roscadora"))
                                    {
                                        currentDGRow.Extremo = reportConfirmation.Extremo;
                                        GetSequenceForDifferentExtreme(currentDGRow, reportProductionDto);
                                        reportProductionDto.Operacion = GetOperation(currentDGRow, reportProductionDto);
                                    }
                                    Login();
                                    response = ReportExecute(showErrorMessageRequest, showMessageRequest, showQuestionRequest, reportConfirmation);
                                }
                                else
                                    response = false;

                            }
                            else
                            {
                                ShowError showError = new ShowError("Error", string.Format("No puede reportar la producción de una máquina distinta a {0}", maquina));
                                showErrorMessageRequest.Raise(new Notification() { Content = showError });
                            }
                        }
                        else
                        {
                            ShowError showError = new ShowError("Error", string.Format("Debe reportar el parcial anterior. Operación cancelada"));
                            showErrorMessageRequest.Raise(new Notification() { Content = showError });
                        }


                    }
                    else
                    {
                        ShowError showError = new ShowError("Error", string.Format("Este reporte ya ha sido enviado. Operación cancelada"));
                        showErrorMessageRequest.Raise(new Notification() { Content = showError });
                    }
                }
            }
            else if (cancel)
                return response;
            else
            {
                ShowError showError = new ShowError("Error", string.Format("No se pudo iniciar sesión en el sistema. Operación cancelada"));
                showErrorMessageRequest.Raise(new Notification() { Content = showError });
            }

            //REP - Bloqueamos nuevamente el Grid para que sea necesario que ingresen nuevamente la clave de supervisor.

            return response;
        }
        private static bool ReportV3(GeneralPiece currentDGRow, InteractionRequest<Notification> request,
            InteractionRequest<Notification> showErrorMessageRequest, InteractionRequest<Notification> showMessageRequest,
            InteractionRequest<Notification> showQuestionRequest, InteractionRequest<Notification> IndBoxReportConfirmationRequest)
        {
            //string AssemblyName = $"{Assembly.GetCallingAssembly().Location}\\Adapter";
            //Assembly assembly = Assembly.LoadFrom("Adapter\\Tenaris.Fava.Production.Reporting.SecurityAdapter.dll");
            //if(assembly.GetType("SecurityAd") != null)
            //{
            //    //Adapter = (ISecurityAdapter)assembly.CreateInstance("Tenaris.Fava.Production.Reporting.SecurityAdapter.SecurityAd");                
            //}
            bool response = false;
            if (Login())
            {
                bool cancel;
                var reportProductionDto = GetCurrentGroupItemToReport(currentDGRow);
                if (reportProductionDto != null)
                {
                    string maquina = Configurations.Instance.Machine;
                    GeneralPiece generalPieceDto = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                    bool byPass = ConfigurationManager.AppSettings["Bypass"] == "true" ? true : false;

                    UpdateReportBoxFlag(generalPieceDto);

                    if (maquina.Contains("Roscadora") /*&& generalPieceDto.Extremo.EndsWith("2")*/ && !string.IsNullOrEmpty(generalPieceDto.ReportBox) && generalPieceDto.ReportBox != "N")
                    {
                        byPass = true;
                    }

                    if (reportProductionDto.Enviado == Enumerations.AxlrBit.No || byPass || Configurations.Instance.Machine.Contains("Forjadora"))
                    {
                        if (ValidationRules.ValidateReportSequence(currentGeneraPieces, generalPieceDto))
                        {
                            //if (maquina.Contains(reportProductionDto.DescripcionMaquina))
                            if (maquina.ToString().Contains(reportProductionDto.DescripcionMaquina) || reportProductionDto.DescripcionMaquina.Contains("Roscadora"))
                            {
                                if (maquina.Contains("Forjadora"))
                                {

                                    string user = whoIsLogged;
                                    ReportConfirmationViewModel reportConfirmation = new ReportConfirmationViewModel(generalPieceDto, reportProductionDto, GetFirstPieceLoadedNumberForIT(generalPieceDto), user);
                                    Trace.Message("reportConfirmation:GoodCount:{0},ScrapCount:{1},LoadedCount:{2}---> user:{3}", generalPieceDto.GoodCount, generalPieceDto.ScrapCount, generalPieceDto.LoadedCount);
                                    request.Raise(new Notification() { Content = reportConfirmation });
                                    if (reportConfirmation.Result)
                                        response = ReportExecute(showErrorMessageRequest, showMessageRequest, showQuestionRequest, reportConfirmation);

                                }

                                else if ((maquina.Contains("Roscadora") && generalPieceDto.Extremo.EndsWith("2") && !string.IsNullOrEmpty(generalPieceDto.ReportBox) && generalPieceDto.ReportBox != Configurations.Instance.FlagITNOReportBox))/*ConfigurationManager.AppSettings["FlagITNOReportBox"].ToString()*/ //|| Configurations.Instance.VersionApplication == "V4"
                                {
                                    //Para Mecanizado
                                    string user = whoIsLogged;
                                    IndBoxReportConfirmationViewModel reportConfirmation = new IndBoxReportConfirmationViewModel(generalPieceDto, reportProductionDto, user);
                                    Trace.Message("IndBoxReportConfirmationViewModel:GoodCount:{0},ScrapCount:{1},LoadedCount:{2}---> user:{3}", generalPieceDto.GoodCount, generalPieceDto.ScrapCount, generalPieceDto.LoadedCount);
                                    IndBoxReportConfirmationRequest.Raise(new Notification() { Content = reportConfirmation });
                                    // TO-DO: Implementar dialogo en confirmacion de reporte de cajas
                                    // Se debe incluir un bool llamado result en el que despues de presionar
                                    // El boton de aceptar devuelva un true, y false en caso de presionar cancelar
                                    if (reportConfirmation.Result)
                                        response = ReportExecute(showErrorMessageRequest, showMessageRequest, showQuestionRequest, null, reportConfirmation); // response = reportConfirmation.Result;
                                }
                                else
                                {
                                    //Las Demas Estaciones                                    
                                    string user = whoIsLogged;

                                    ReportConfirmationViewModel reportConfirmation = new ReportConfirmationViewModel(generalPieceDto, reportProductionDto, GetFirstPieceLoadedNumberForIT(generalPieceDto), user);
                                    Trace.Message("reportConfirmation:GoodCount:{0},ScrapCount:{1},LoadedCount:{2}---> user:{3}", generalPieceDto.GoodCount, generalPieceDto.ScrapCount, generalPieceDto.LoadedCount);
                                    request.Raise(new Notification() { Content = reportConfirmation });
                                    if (reportConfirmation.Result)
                                        response = ReportExecute(showErrorMessageRequest, showMessageRequest, showQuestionRequest, reportConfirmation);
                                }
                            }
                            else
                            {
                                ShowError showError = new ShowError("Error", string.Format("La estación de trabajo no corresponde a la estación de la pieza. Operación cancelada"));
                                showErrorMessageRequest.Raise(new Notification() { Content = showError });
                            }
                        }
                        else
                        {
                            ShowError showError = new ShowError("Error", string.Format("Debe reportar el parcial anterior. Operación cancelada"));
                            showErrorMessageRequest.Raise(new Notification() { Content = showError });
                        }
                    }
                    else
                    {
                        if (maquina.Contains("Pintado"))
                        {
                            ShowQuestion showQuestion = new ShowQuestion("Confirmar Reporte Para Embalado", "El registro seleccionado ya está completo como pintado, ¿Desea generar el envío a Embalado?");
                            showQuestionRequest.Raise(new Notification() { Content = showQuestion });
                            if (showQuestion.Result)
                            {

                                //Las Demas Estaciones
                                string user = whoIsLogged;

                                reportProductionDto.Almacen = "de pintado";
                                reportProductionDto.DescripcionMaquina = "Embalado Bateria 1";
                                reportProductionDto.Enviado = Enumerations.AxlrBit.No;
                                reportProductionDto.Opcion = "Embalado";
                                reportProductionDto.Operacion = "Embalado";
                                reportProductionDto.Secuencia = 12;

                                //generalPieceDto.

                                ReportConfirmationViewModel reportConfirmation = new ReportConfirmationViewModel(generalPieceDto, reportProductionDto, GetFirstPieceLoadedNumberForIT(generalPieceDto), user);
                                request.Raise(new Notification() { Content = reportConfirmation });
                                response = reportConfirmation.Result;

                            }
                        }
                        else
                        {
                            ShowError showError = new ShowError("Error", string.Format("Este reporte ya ha sido enviado. Operación cancelada"));
                            showErrorMessageRequest.Raise(new Notification() { Content = showError });
                        }
                    }
                }
            }
            //Adapter.Notify -= new EventHandler<ChangedUserEventArgs>(Security_Notify);
            //Adapter.Uninitialize();
            //Adapter.Dispose();
            return response;
            //return true;
        }
        //
        public static void UpdateReportBoxFlag(GeneralPiece GeneralPiece)
        {
            //comentado para que siempre busque su tipo de reporte en TPS
            //if (string.IsNullOrEmpty(GeneralPiece.ReportBox))
            //{
            int seq;

            if (GeneralPiece.Extremo.ToString() != string.Empty)
            {
                if (GeneralPiece.Extremo.EndsWith("2"))
                    seq = Convert.ToInt32(Configurations.Instance.Secuencia) + 1;
                else
                    seq = Convert.ToInt32(Configurations.Instance.Secuencia);
            }
            else
            {
                seq = Convert.ToInt32(Configurations.Instance.Secuencia);
            }

            ITServiceAdapter business = new ITServiceAdapter();
            Trace.Message("OrderNumber: {0}, seq: {1}", GeneralPiece.OrderNumber.ToString(), seq);
            System.Data.DataTable dt = business.GetAvailableStock(GeneralPiece.OrderNumber.ToString(), seq);
            //Trace.Message("dt:{0},{1},{2},{3}", dt.Rows[0].ToString(), dt.Rows[1].ToString(), dt.Rows[2].ToString(), dt.Rows[3].ToString());

            //if (dt.Rows.Count > 0)
            if (dt.Columns.Contains("IdUdt"))
            {
                System.Data.DataRow[] drs = dt.Select("IdUdt = '" + GeneralPiece.GroupItemNumber.ToString() + "'");
                Trace.Message("drs: {0}", drs.ToString());

                if (drs.Count() > 0)
                {
                    GeneralPiece.ReportBox = drs[0]["ProductReportBox"].ToString();
                }
            }
            //}
        }


        //public Enumerations.ProductionReportSendStatus GetSendStatus()
        //{
        //    var currentDGRow = dgReporteProduccion.CurrentRow;
        //    var GeneralPiece = GetCurrentGeneralPiece(Convert.ToInt32(currentDGRow.Cells[0].Value.ToString()));
        //    return GeneralPiece.SendStatus;
        //}



        public static ReportProductionDto GetCurrentGroupItemToReport(GeneralPiece currentDGRow)
        {
            ReportProductionDto reportProductionDto = null;

            if (currentDGRow != null)
            {
                var GeneralPiece = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                reportProductionDto = new ReportProductionDto()
                {
                    //V1: TipoUDT = "Tarjeta de Linea",
                    TipoUDT = string.IsNullOrEmpty(GeneralPiece.GroupItemType) ? "Tarjeta de Linea" : GeneralPiece.GroupItemType,
                    IdBatch = GeneralPiece.IdBatch,
                    IdHistory = GeneralPiece.IdHistory,//Convert.ToInt32(currentDGRow.Cells[0].Value.ToString()),
                    Orden = GeneralPiece.OrderNumber,//Convert.ToInt32(currentDGRow.Cells["OrderNumber"].Value.ToString()),
                    Almacen = GeneralPiece.Location,//currentDGRow.Cells["Location"].Value.ToString(),
                    IdUDT = GeneralPiece.GroupItemNumber,//Convert.ToInt32(currentDGRow.Cells["GroupItemNumber"].Value.ToString()),
                    Colada = GeneralPiece.HeatNumber,//Convert.ToInt32(currentDGRow.Cells["HeatNumber"].Value.ToString()),
                    Lote = GeneralPiece.LotNumberHTR, //String.IsNullOrEmpty(currentDGRow.Cells["LotNumberHTR"].Value.ToString()) ? 0 :
                                                      //Convert.ToInt32(currentDGRow.Cells["LotNumberHTR"].Value.ToString()),
                    Aprietes = 0,
                    DescripcionMaquina = GeneralPiece.Description,
                    CantidadMalas = GeneralPiece.ScrapCount,//Convert.ToInt32(currentDGRow.Cells["ScrapCount"].Value.ToString()),
                    CantidadBuenas = GeneralPiece.GoodCount,//Convert.ToInt32(currentDGRow.Cells["GoodCount"].Value.ToString()),
                    CantidadReprocesadas = GeneralPiece.ReworkedCount,// Convert.ToInt32(currentDGRow.Cells["ReworkedCount"].Value.ToString()),
                                                                      // Observaciones = "",
                    Enviado = GeneralPiece.Sended,
                    //CantidadProcesadas = Convert.ToInt32(currentDGRow.Cells["LoadedCount"].Value.ToString()) -
                    //                Convert.ToInt32(currentDGRow.Cells["ReworkedCount"].Value.ToString()),
                    /**TO-DO**/
                    // Hay que ir a la tabla que contiene los datos del atado, es decir el número de piezas, el 
                    //número de cantidad de piezas procesadas siempre debe ser <= a la Cantidad Total
                    // Cantidad Procesadas = Cantidad buenas + descartes
                    CantidadTotal = GeneralPiece.LoadedCount //- GeneralPiece.ReworkedCount

                };

                GetMachineInformation(ref reportProductionDto, currentDGRow);
            }
            return reportProductionDto;
        }

        public static void GetMachineInformation(ref ReportProductionDto reportProductDto, GeneralPiece currentDGRow)
        {
            var generalPiece = currentDGRow;
            string maquina = generalPiece.Description;
            //string maquina = dgReporteProduccion.CurrentRow.Cells["Description"].Value.ToString();

            //reportProductDto.Secuencia =
            GetSequenceForDifferentExtreme(generalPiece, reportProductDto);
            reportProductDto.Operacion = GetOperation(generalPiece, reportProductDto);
            reportProductDto.Opcion = Configurations.Instance.Opcion;
        }


        public static string GetOperation(GeneralPiece GeneralPiece, ReportProductionDto reportProductDto)
        {
            string operation = Configurations.Instance.Operacion;
            if (GeneralPiece.Extremo.ToString() != String.Empty)
            {
                //Hard Code por reporte de las dos operaciones en mismo colgante 
                if (reportProductDto.DescripcionMaquina.ToUpper().Contains("FORJADORA"))
                    operation = "Forjado " + GeneralPiece.Extremo;
                else if (reportProductDto.DescripcionMaquina.Contains("Roscadora"))
                    operation = "Mecanizado " + GeneralPiece.Extremo;

            }
            return operation;
        }

        public static void GetSequenceForDifferentExtreme(GeneralPiece GeneralPiece, ReportProductionDto reportProductDto)
        {
            if (GeneralPiece.Extremo.ToString() != string.Empty)
            {
                if (GeneralPiece.Extremo.EndsWith("2"))
                    reportProductDto.Secuencia = Convert.ToInt32(Configurations.Instance.Secuencia) + 1;
                else
                    reportProductDto.Secuencia = Convert.ToInt32(Configurations.Instance.Secuencia);
            }
            else
                reportProductDto.Secuencia = Convert.ToInt32(Configurations.Instance.Secuencia);
        }

        public static ObservableCollection<ReportProductionHistory> dgReporteProduccion_SelectionChanged(GeneralPiece currentDGRow)
        {

            if (currentDGRow != null)
            {
                var generalpiece = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                if (generalpiece != null)
                {
                    //tssLbl.Text = generalpiece.Customer;
                    ObservableCollection<ReportProductionHistory> productionReportHistories = new ReportProductionHistoryFacade().GetReportProductionHistoryByParams
                        (generalpiece.OrderNumber, generalpiece.GroupItemNumber,
                        generalpiece.HeatNumber, null, null, null);

                    return new ObservableCollection<ReportProductionHistory>(productionReportHistories);
                }
            }
            return null;
        }


        //nuevos cambios para forja 0
        public static GeneralPiece dgReporteProduccion_SelectionChangedForja0(GeneralPiece currentDGRow)
        {

            if (currentDGRow != null)
            {
                var generalpiece = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                if (generalpiece != null)
                {
                    //tssLbl.Text = generalpiece.Customer;
                    ObservableCollection<ReportProductionHistoryV1> productionReportHistories = new ReportProductionHistoryFacade().GetReportProductionHistoryByParamsV1
                        (generalpiece.OrderNumber, generalpiece.GroupItemNumber,
                        generalpiece.HeatNumber, null, null, null);

                    // OPERACIONES PRUEBA

                    //buenasReportadas = GoodCount
                    //DescartesR = ScrapCount
                    //Total Reportadas = buenasReportadas + Descartes R
                    //Pendientes = Cargados - Total Reportadas


                    //if (Pendientes != 0)
                    //{ GoodCount = GoodCount - BuenasReportadas, ScrapCount = ScrapCount - DescartesR, LoadedCount = Total Reportadas }
                    //else
                    //{
                    //    GoodCount = 0, Scrapcount = 0, Loadedcount = 0}





                    //OPERACION FORJA0
                    if (Configurations.Instance.Machine == "Forjadora 0")
                    {
                        int num1 = 0;
                        int num2 = 0;
                        string machineOption = Configurations.Instance.Opcion;
                        // string machineOperation = productionReportHistories.Where(x => x.MachineOperation.Contains(currentDGRow.Extremo)).LastOrDefault().MachineOperation;

                        foreach (ReportProductionHistoryV1 productionHistory in productionReportHistories)
                        {
                            if (productionHistory.MachineOperation.Contains(currentDGRow.Extremo))
                            {
                                num1 += productionHistory.GoodCount;
                                num2 += productionHistory.ScrapCount;
                            }

                        }

                        currentDGRow.BuenasReportadas = num1;
                        currentDGRow.MalasReportadas = num2;
                        currentDGRow.TotalReportado = (num1 + num2);
                        currentDGRow.PendientesPorReportar = currentDGRow.LoadedCount - (num1 + num2);
                        if (currentDGRow.PendientesPorReportar != 0)
                        {
                            currentDGRow.GoodCount = currentDGRow.GoodCount - num1;
                            currentDGRow.ScrapCount = currentDGRow.ScrapCount - num2;
                            currentDGRow.LoadedCount = currentDGRow.GoodCount + currentDGRow.ScrapCount;
                        }
                        else
                        {
                            currentDGRow.GoodCount = 0;
                            currentDGRow.ScrapCount = 0;
                            currentDGRow.LoadedCount = 0;
                        }


                        currentDGRow.Cargados = currentDGRow.BuenasReportadas + currentDGRow.MalasReportadas;
                    }


                    return currentDGRow;
                }
            }
            return null;
        }



        public static ObservableCollection<ReportProductionHistoryV1> dgReporteProduccion_SelectionChangedV1(GeneralPiece currentDGRow)
        {

            if (currentDGRow != null)
            {
                var generalpiece = GetCurrentGeneralPiece(currentDGRow.IdHistory);
                if (generalpiece != null)
                {
                    //tssLbl.Text = generalpiece.Customer;
                    ObservableCollection<ReportProductionHistoryV1> productionReportHistories = new ReportProductionHistoryFacade().GetReportProductionHistoryByParamsV1
                        (generalpiece.OrderNumber, generalpiece.GroupItemNumber,
                        generalpiece.HeatNumber, null, null, null);

                    return new ObservableCollection<ReportProductionHistoryV1>(productionReportHistories);
                }
            }
            return null;
        }

        public static void InitialMachineZone_CheckedChanged(bool isInitialMachine)
        {
            Configurations.Instance.MaquinaInicialZona = (isInitialMachine) ? "1" : "0";

        }
        public static bool Login()
        {

            if (ConfigurationManager.AppSettings["UserByPass"] == "")
            {
                whoIsLogged = ProductionReport.GetCurrentUser();
                if (string.IsNullOrEmpty(whoIsLogged))
                    return false;
                return true;
            }
            else
            {
                whoIsLogged = ConfigurationManager.AppSettings["UserByPass"];
                return true;
            }

        }



        /* V1: 
                private void Form1_FormClosed(object sender, FormClosedEventArgs e)
                {
                    if (oplSubscription != null)
                        oplSubscription.CloseSession();
                }
        */


        public static GeneralPiece ExtremoForja(InteractionRequest<Notification> ShowQuestionRequest, GeneralPiece generalPiece)
        {
            if (generalPiece.Extremo == ext1)
            {
                var confirmMessage = string.Format("¿Desea CAMBIAR el EXTREMO y reportar el Atado {0} como {1}?", generalPiece.GroupItemNumber, ext2);
                //Pregunta si reportamos como extremo 1 o 2
                ShowQuestion showQuestion = new ShowQuestion("Confirmar reporte", confirmMessage);
                ShowQuestionRequest.Raise(new Notification() { Content = showQuestion });
                if (showQuestion.Result)
                {
                    generalPiece.Extremo = ext2;
                    return generalPiece;
                }
                else
                    return generalPiece;
            }
            else if (generalPiece.Extremo == ext2)
            {
                var confirmMessage = string.Format("¿Desea CAMBIAR el EXTREMO y reportar el Atado {0} como {1}?", generalPiece.GroupItemNumber, ext1);
                //Pregunta si reportamos como extremo 1 o 2
                ShowQuestion showQuestion = new ShowQuestion("Confirmar reporte", confirmMessage);
                ShowQuestionRequest.Raise(new Notification() { Content = showQuestion });
                if (showQuestion.Result)
                {
                    generalPiece.Extremo = ext1;
                    return generalPiece;
                }
                else
                    return generalPiece;
            }
            return generalPiece;

        }

        public static bool Unlock(string username, string password)
        {
            return UserFacade.Login(username, password);
        }


        #endregion

    }
}
