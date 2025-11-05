namespace facturas.Components.Data
{
    public class Factura
    {

        public DateOnly Fecha { get; set; }
        public string Nombre { get; set; }

        public string Producto { get; set; }

        public double Precio { get; set; }
    }
}
