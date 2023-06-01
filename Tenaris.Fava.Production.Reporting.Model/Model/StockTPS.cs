namespace Tenaris.Fava.Production.Reporting.Model.Model
{
    public class StockTPS
    {

        public string Colada { get; set; }
        public string CodigoColada { get; set; }
        public string TipoUdt { get; set; }
        public string IdUdt { get; set; }
        public string Lote { get; set; }
        public string Cantidad { get; set; }
        public string Almacen { get; set; }
        public string Extremo { get; set; }
        public string SecuenciaSiguiente { get; set; }
        public string OperacionSiguiente { get; set; }
        public string OpcionSiguiente { get; set; }
        public string Lot4 { get; set; }
        public string LotId { get; set; }
        public string ProductReportBox { get; set; }
        public string Order { get; set; }
        public string TipoUdc { get; set; }

        public StockTPS()
        {

        }

        public StockTPS(string order,
            string colada,
            string codigoColada,
            string tipoUdt,
            string idUdt,
            string tipoUdc,
            string lote,
            string cantidad,
            string almacen,
            string extremo,
            string secuenciaSiguiente,
            string operacionSiguiente,
            string opcionSiguiente,
            string lot4,
            string lotId,
            string productBoxReport)
        {
            this.Order = order;
            this.Colada = colada;
            this.CodigoColada = codigoColada;
            this.TipoUdt = tipoUdt;
            this.IdUdt = idUdt;
            this.TipoUdc = tipoUdc;
            this.Lote = lote;
            this.Cantidad = cantidad;
            this.Almacen = almacen;
            this.Extremo = extremo;
            this.SecuenciaSiguiente = secuenciaSiguiente;
            this.OperacionSiguiente = operacionSiguiente;
            this.OpcionSiguiente = opcionSiguiente;
            this.Lot4 = lot4;
            this.LotId = lotId;
            this.ProductReportBox = productBoxReport;
        }



    }

    public interface IStockBuilder
    {

        IStockBuilder WithColada(string value);
        IStockBuilder WithCodigoColada(string value);
        IStockBuilder WithTipoUdt(string value);
        IStockBuilder WithIdUdt(string value);
        IStockBuilder WithLote(string value);
        IStockBuilder WithCantidad(string value);
        IStockBuilder WithAlmacen(string value);
        IStockBuilder WithExtremo(string value);
        IStockBuilder WithSecuenciaSiguiente(string value);
        IStockBuilder WithOperacionSiguiente(string value);
        IStockBuilder WithOpcionSiguiente(string value);
        IStockBuilder WithLot4(string value);
        IStockBuilder WithLotId(string value);
        IStockBuilder WithProductReportBox(string value);
        IStockBuilder WithOrder(string value);
        IStockBuilder WithTipoUdc(string value);
        StockTPS Build();

    }

    public class StockTPSBuilder : IStockBuilder
    {
        internal StockTPS IStockTPS;

        public StockTPSBuilder()
        {
            IStockTPS = new StockTPS();
        }

        public static IStockBuilder Create()
        {
            return new StockTPSBuilder();
        }

        public StockTPS Build()
        {
            return IStockTPS;
        }

        public IStockBuilder WithAlmacen(string value)
        {
            IStockTPS.Almacen = value;
            return this;
        }

        public IStockBuilder WithCantidad(string value)
        {
            IStockTPS.Cantidad = value;
            return this;
        }

        public IStockBuilder WithCodigoColada(string value)
        {
            IStockTPS.CodigoColada = value;
            return this;
        }

        public IStockBuilder WithColada(string value)
        {
            IStockTPS.Colada = value;
            return this;
        }

        public IStockBuilder WithExtremo(string value)
        {
            IStockTPS.Extremo = value;
            return this;
        }

        public IStockBuilder WithIdUdt(string value)
        {
            IStockTPS.IdUdt = value;
            return this;
        }

        public IStockBuilder WithLot4(string value)
        {
            IStockTPS.Lot4 = value;
            return this;
        }
        public IStockBuilder WithLotId(string value)
        {
            IStockTPS.LotId = value;
            return this;
        }

        public IStockBuilder WithLote(string value)
        {
            IStockTPS.Lote = value;
            return this;
        }

        public IStockBuilder WithOpcionSiguiente(string value)
        {
            IStockTPS.OpcionSiguiente = value;
            return this;
        }

        public IStockBuilder WithOperacionSiguiente(string value)
        {
            IStockTPS.OperacionSiguiente = value;
            return this;
        }

        public IStockBuilder WithOrder(string value)
        {
            IStockTPS.Order = value;
            return this;
        }

        public IStockBuilder WithProductReportBox(string value)
        {
            IStockTPS.ProductReportBox = value;
            return this;
        }

        public IStockBuilder WithSecuenciaSiguiente(string value)
        {
            IStockTPS.SecuenciaSiguiente = value;
            return this;
        }

        public IStockBuilder WithTipoUdc(string value)
        {
            IStockTPS.TipoUdc = value;
            return this;
        }

        public IStockBuilder WithTipoUdt(string value)
        {
            IStockTPS.TipoUdt = value;
            return this;
        }
    }

}
