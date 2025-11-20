using System.Globalization;

namespace facturas.Components.Data
{
    public class DatosDashboard
    {
        public string TopClienteNombre { get; set; } = "N/A";
        public decimal TopClienteTotal { get; set; } = 0;

        public string TopMesNumero { get; set; } = string.Empty;
        public decimal TopMesTotal { get; set; } = 0;

        public string TopProductoNombre { get; set; } = "N/A";
        public int TopProductoCantidad { get; set; } = 0;

        public string NombreMes
        {
            get
            {
                if (int.TryParse(TopMesNumero, out int numMes))
                {
                    return new CultureInfo("es-ES").DateTimeFormat.GetMonthName(numMes).ToUpper();
                }
                return "N/A";
            }
        }
        public decimal TicketPromedio { get; set; } = 0;

        public int TotalFacturasEmitidas { get; set; } = 0;

        public List<Factura> UltimasFacturas { get; set; } = new List<Factura>();
    }
}
