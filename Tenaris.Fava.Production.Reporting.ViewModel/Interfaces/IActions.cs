using Tenaris.Fava.Production.Reporting.Model.Adapter;
using Tenaris.Fava.Production.Reporting.ViewModel.Strategy;

namespace Tenaris.Fava.Production.Reporting.ViewModel.Interfaces
{
    public interface IActions
    {
        ITServiceAdapter Adapter { get; set; }
        GeneralMachine GeneralMachine { get; }
        void Search();
    }
}