namespace facturas.Components.Data
{
    public class Producto
    {
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; } = 0; 
        public int Cantidad { get; set; } = 1; 

        public decimal TotalItem
        {
            get { return PrecioUnitario * Cantidad; }
        }
    }
}
