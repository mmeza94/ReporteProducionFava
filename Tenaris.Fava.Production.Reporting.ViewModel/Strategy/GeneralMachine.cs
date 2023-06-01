using Microsoft.Practices.Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using System.Configuration;
using Tenaris.Fava.Production.Reporting.Model.Adapter;
using Tenaris.Fava.Production.Reporting.Model.DTO;
using Tenaris.Fava.Production.Reporting.Model.Support;
using Tenaris.Fava.Production.Reporting.ViewModel.Dialog;

namespace Tenaris.Fava.Production.Reporting.ViewModel.Strategy
{
    public abstract class GeneralMachine
    {
        public string WhoIsLogged { get; set; }
        public ITServiceAdapter Adapter { get; set; }
        public IList<GeneralPiece> CurrentGeneralPieces { get; set; }
        public InteractionRequest<Notification> Request, IndBoxReportConfirmationRequest,
            ShowErrorMessageRequest, ShowMessageRequest, ShowQuestionRequets;

        public GeneralMachine()
        {
            Adapter = new ITServiceAdapter();
        }

        public bool Login()
        {
            if (!(ConfigurationManager.AppSettings["UserByPass"] == ""))
            {
                WhoIsLogged = ConfigurationManager.AppSettings["UserByPass"];
                return true;
            }

            WhoIsLogged = ProductionReport.GetCurrentUser();

            if (string.IsNullOrEmpty(WhoIsLogged))
            {
                ShowErrorMessageRequest.Raise(new Notification()
                {
                    Content = new ShowError("Error",
                    string.Format("No se pudo iniciar sesión en el sistema. Operación cancelada"))
                });
                return false;
            }
            return true;
        }


    }
}
