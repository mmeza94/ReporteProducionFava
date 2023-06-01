
using System;
using Tenaris.Fava.Production.Reporting.Model.Adapter;
using Tenaris.Fava.Production.Reporting.ViewModel.Interfaces;

namespace Tenaris.Fava.Production.Reporting.ViewModel.Strategy
{
    public class PaintingStrategy : IActions
    {
        public ITServiceAdapter Adapter { get; set ; }

        public GeneralMachine GeneralMachine { get; }

        public void Search()
        {
            Console.WriteLine("Este es un proceso");
        }
    }
}
