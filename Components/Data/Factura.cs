namespace facturas.Components.Data
{
    public class Factura
    {

        public int FacturaID { get; set; }
        public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public string Nombre { get; set; } = string.Empty;
        public List<Producto> Productos { get; set; } = new List<Producto>();

        private decimal _totalFactura;
        public decimal TotalFactura
        {
            get
            {

                if (Productos.Any()) 
                {
                    return Productos.Sum(p => p.TotalItem);
                }
                return _totalFactura;
            }
            set
            {
                _totalFactura = value;
            }
        }

    }
}
