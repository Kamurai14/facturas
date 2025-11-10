namespace facturas.Components.Data
{
    public class Factura
    {

        public DateOnly Fecha { get; set; }
        public string Nombre { get; set; }
        public List<Producto> Productos { get; set; }

    }
}
