using System.Globalization;

namespace facturas.Components.Data
{
    public class ReporteMensual
    {
        public string Mes { get; set; } = string.Empty;

        public decimal TotalMes { get; set; }

        public string NombreMes
        {
            get
            {
                if (int.TryParse(Mes, out int numMes))
                {
                    var cultura = new CultureInfo("es-ES");
                    string nombre = cultura.DateTimeFormat.GetMonthName(numMes);
                    return nombre.ToUpper();
                }
                return "Mes Desconocido";
            }
        }
    }
}
