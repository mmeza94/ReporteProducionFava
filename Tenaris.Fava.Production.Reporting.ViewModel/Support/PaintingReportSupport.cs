using log4net;
using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using Tenaris.Fava.Production.Reporting.Model.Adapter;
using Tenaris.Fava.Production.Reporting.Model.Business;
using Tenaris.Fava.Production.Reporting.Model.DTO;
using Tenaris.Fava.Production.Reporting.Model.Model;
using Tenaris.Fava.Production.Reporting.Model.Support;
using Tenaris.Fava.Production.Reporting.ViewModel.Dialog;

namespace Tenaris.Fava.Production.Reporting.ViewModel.Support
{
    public class PaintingReportSupport
    {

        public static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static int reportedPieces;
        public PaintingReportSupport()
        {
            //InitializeComponent();

            log4net.Config.XmlConfigurator.Configure();
            System.Runtime.Remoting.RemotingConfiguration.Configure(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile, false);
            log.Debug("Starting...");
            //this.Text = String.Format("Reporte de Producción - {0} {1}", ConfigurationManager.AppSettings["Machine"].ToString(), "");
            //ToolTips();
        }

        public static void dgStockParaTPS(int cajon,
            out ObservableCollection<StockTPS> StockParaTPSRef,
            out ObservableCollection<BoxLoad> CajasCargadasRef,
            out ObservableCollection<BoxReport> ReportesDeCajaRef)
        {
            StockParaTPSRef = new ObservableCollection<StockTPS>();

            ObservableCollection<BoxReport> dt = ProductionReportingBusiness.GetBoxesForPainting(cajon),
                filteredTable = new ObservableCollection<BoxReport>(dt.Where(Box => Box.MachineOperation.Equals("Mecanizado Extremo 2"))),
                reported = new ObservableCollection<BoxReport>(dt.Where(Box => Box.MachineOperation.Equals("Pintado")));
            ObservableCollection<BoxLoad> dtBaseDeDatos = ProductionReportingBusiness.GetLoadPainting(cajon);

            if (filteredTable.Count > 0)
                StockParaTPSRef = new ObservableCollection<StockTPS>(new ITServiceAdapter().GetAvailableStock(filteredTable.FirstOrDefault().OrdenHija, 11)
                    .Select($"IdUdt = '{ cajon}'")
                    .Select(row => StockTPS(row))
                    .ToList()
                    );

            if (reported.Count() > 0)
                reportedPieces = reported.Sum(Box => Convert.ToInt32(Box.PiezasBuenas));

            CajasCargadasRef = dtBaseDeDatos;
            ReportesDeCajaRef = dt;
        }

        private static PaintingReport GetCurrentGroupItemToReport(int option,
            StockTPS SelectedTPS,
            BoxLoad SelectedLoaded)
        {

            PaintingReport reportProductionDto = null;
            //DataGridViewRow currentDGRow = null;
            StockTPS currentDGRow1 = null;
            BoxLoad currentDGRow2 = null;
            int totales = 0;

            if (option == 1)
            {
                //Load

                currentDGRow1 = SelectedTPS;
                currentDGRow2 = null;
            }
            else
            {
                //Report

                currentDGRow2 = SelectedLoaded;
                currentDGRow1 = null;
            }

            if (currentDGRow1 != null)
            {
                if (option == 1)
                {
                    totales = Convert.ToInt32(currentDGRow1.Cantidad);
                }


                reportProductionDto = new PaintingReport()
                {
                    ChildOrden = Convert.ToInt32(currentDGRow1.Order),
                    ParentOrden = Convert.ToInt32(currentDGRow1.Order),
                    HeatNumber = Convert.ToInt32(currentDGRow1.Colada),
                    HeatNumberCode = currentDGRow1.CodigoColada,
                    UdtType = currentDGRow1.TipoUdt,
                    BoxUdt = currentDGRow1.IdUdt,
                    ParentUdt = currentDGRow1.IdUdt,
                    UdcType = currentDGRow1.TipoUdc,
                    LotId = currentDGRow1.Lote,
                    LoadQuantity = totales,//Convert.ToInt32(currentDGRow.Cells["Cantidad"].Value.ToString()),
                    Storage = currentDGRow1.Almacen,
                    NextSequence = Convert.ToInt32(currentDGRow1.SecuenciaSiguiente),
                    NextOperation = ConfigurationManager.AppSettings["Operation_" + Configurations.Instance.Secuencia.ToString()].ToString(),//currentDGRow.Cells["OperacionSiguiente"].Value.ToString(),
                    NextOption = ConfigurationManager.AppSettings["Option_" + Configurations.Instance.Secuencia.ToString()].ToString(), //currentDGRow.Cells["OpcionSiguiente"].Value.ToString()
                    GoodCount = totales


                };


            }


            else if (currentDGRow2 != null)
            {
                if (option == 2)
                {
                    totales = Convert.ToInt32(currentDGRow2.Cantidad) - reportedPieces;
                }


                reportProductionDto = new PaintingReport()
                {
                    ChildOrden = Convert.ToInt32(currentDGRow2.Order),

                    ParentOrden = Convert.ToInt32(currentDGRow2.Order),
                    HeatNumber = Convert.ToInt32(currentDGRow2.Colada),
                    HeatNumberCode = currentDGRow2.CodigoColada,
                    UdtType = currentDGRow2.TipoUdt,
                    BoxUdt = currentDGRow2.IdUdt,
                    ParentUdt = currentDGRow2.IdUdt,
                    UdcType = currentDGRow2.TipoUdc,

                    LotId = currentDGRow2.Lote,
                    LoadQuantity = totales,//Convert.ToInt32(currentDGRow.Cells["Cantidad"].Value.ToString()),
                    Storage = currentDGRow2.Almacen,
                    NextSequence = Convert.ToInt32(currentDGRow2.SecuenciaSiguiente),


                    NextOperation = ConfigurationManager.AppSettings["Operation_" + Configurations.Instance.Secuencia.ToString()].ToString(),
                    /* NextOperation = "Inyectado",*///currentDGRow.Cells["OperacionSiguiente"].Value.ToString(),
                    /*NextOption = "Inyectado",*/ //currentDGRow.Cells["OpcionSiguiente"].Value.ToString()

                    NextOption = ConfigurationManager.AppSettings["Option_" + Configurations.Instance.Secuencia.ToString()].ToString(),
                    GoodCount = totales


                };
            }



            else
            {
                return null;
            }
            return reportProductionDto;
        }

        public static void btnReportPintado_Click(int cajon,
            out ObservableCollection<StockTPS> StockParaTPSRef,
            out ObservableCollection<BoxLoad> CajasCargadasRef,
            out ObservableCollection<BoxReport> ReportesDeCajaRef,
            BoxLoad SelectedLoaded,
            InteractionRequest<Notification> PaintReportConfirmationRequest,
            InteractionRequest<Notification> ShowErrorMessageRequest,
            InteractionRequest<Notification> ShowMessageRequest,
            InteractionRequest<Notification> ShowQuestionRequest)
        {
            PaintingReport selectedRow = new PaintingReport();
            selectedRow = GetCurrentGroupItemToReport(2, null, SelectedLoaded);

            //security
            ////Las Demas Estaciones
            string user = ProductionReport.GetCurrentUser();
            ////Security.GetCurrentUser().UserID;

            if (selectedRow.LoadQuantity == 0)
            {
                ShowQuestion question = new ShowQuestion("Mensaje de Carga", "La caja ya tiene reportado por Nivel 2 todas las piezas cargadas a Pintado, desea continuar con el envio? ");
                ShowQuestionRequest.Raise(new Notification() { Content = question });
                if (question.Result)

                {
                    PaintingReportConfirmationViewModel reportConfirmation = new PaintingReportConfirmationViewModel(selectedRow, user);
                    PaintReportConfirmationRequest.Raise(new Notification() { Content = reportConfirmation });
                    if (reportConfirmation.Result)
                    {
                        PaintingReportConfirmationSupport.Report(reportConfirmation.DisponiblesTPS, reportConfirmation.CargadasAnterior,
                            reportConfirmation.BuenasActual, reportConfirmation.MalasActual, reportConfirmation.ReprocesosActual,
                                                     reportConfirmation.CargadasActual, reportConfirmation.currentProductionReportDto,
                                                     reportConfirmation.rejectionReportDetails, reportConfirmation.UserReport,
                                                     ShowQuestionRequest, ShowMessageRequest, ShowErrorMessageRequest);
                    }
                    else
                    {
                        dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef);
                        return;
                    }

                    // dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef); //PRUBADWF falta comprobar
                }
                dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef); //PRUBADWF falta comprobar añadido posible
            }
            else
            {

                PaintingReportConfirmationViewModel reportConfirmation = new PaintingReportConfirmationViewModel(selectedRow, user);
                PaintReportConfirmationRequest.Raise(new Notification() { Content = reportConfirmation });
                if (reportConfirmation.Result)
                {
                    PaintingReportConfirmationSupport.Report(reportConfirmation.DisponiblesTPS, reportConfirmation.CargadasAnterior,
                        reportConfirmation.BuenasActual, reportConfirmation.MalasActual, reportConfirmation.ReprocesosActual,
                                                 reportConfirmation.CargadasActual, reportConfirmation.currentProductionReportDto,
                                                 reportConfirmation.rejectionReportDetails, reportConfirmation.UserReport,
                                                 ShowQuestionRequest, ShowMessageRequest, ShowErrorMessageRequest);
                }
                else
                {
                    dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef);
                    return;
                }

                dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef); //PRUBADWF falta comprobar

            }
        }

        public static void btnLoad_Click(int cajon,
            out ObservableCollection<StockTPS> StockParaTPSRef,
            out ObservableCollection<BoxLoad> CajasCargadasRef,
            out ObservableCollection<BoxReport> ReportesDeCajaRef,
            StockTPS SelectedTPS,
            InteractionRequest<Notification> ShowMessageRequest)
        {
            PaintingReport selectedRow = new PaintingReport();
            selectedRow = GetCurrentGroupItemToReport(1, SelectedTPS, null);




            bool isLoaded = false;
            string message = string.Empty;
            isLoaded = new ITServiceAdapter().TPSLoadMaterialForPainting(selectedRow);

            if (isLoaded)
            {
                message = "Caja Cargada correctamente";

            }
            else
            {
                message = "Error al cargar la caja";
            }
            ShowMessage showMessage = new ShowMessage("Mensaje de Carga", message);
            ShowMessageRequest.Raise(new Notification() { Content = showMessage });


            dgStockParaTPS(cajon, out StockParaTPSRef, out CajasCargadasRef, out ReportesDeCajaRef); //PRUEBADWF falta comprobar
        }


        internal static StockTPS StockTPS(DataRow row)
        {
            return StockTPSBuilder
                .Create()
                .WithOrder(row[0].ToString())
                .WithColada(row[1].ToString())
                .WithCodigoColada(row[2].ToString())
                .WithTipoUdt(row[3].ToString())
                .WithIdUdt(row[4].ToString())
                .WithTipoUdc(row[5].ToString())
                .WithLote(row[6].ToString())
                .WithCantidad(row[7].ToString())
                .WithAlmacen(row[8].ToString())
                .WithExtremo(row[9].ToString())
                .WithSecuenciaSiguiente(row[10].ToString())
                .WithOperacionSiguiente(row[11].ToString())
                .WithOpcionSiguiente(row[12].ToString())
                .WithLot4(row[13].ToString())
                .WithLotId(row[14].ToString())
                .WithProductReportBox(row[15].ToString())
                .Build();
        }

    }
}
