using facturas.Components.Data;

namespace facturas.Components.Servicios
{
    public class ServicioControlador
    {
        private readonly ServicioFacturas _servicioFacturas;

        public ServicioControlador(ServicioFacturas servicioFacturas)
        {
            _servicioFacturas = servicioFacturas;
        }       
        public async Task GuardarFacturaAsync(Factura factura)
        {
            await _servicioFacturas.GuardarFacturaAsync(factura);
        }
        public async Task<List<Factura>> ObtenerFacturasAsync()
        {
            return await _servicioFacturas.ObtenerFacturasAsync();
        }

        public async Task<Factura> ObtenerFacturaCompletaAsync(int facturaID) 
        {
            return await _servicioFacturas.ObtenerFacturaCompletaAsync(facturaID);
        }

        public async Task ActualizarFacturaAsync(Factura factura) 
        {
            await _servicioFacturas.ActualizarFacturaAsync(factura);
        }

        public async Task EliminarFacturaAsync(int facturaID) 
        {
            await _servicioFacturas.EliminarFacturaAsync(facturaID);
        }
        public async Task<List<ReporteMensual>> ObtenerReporteAnualAsync(int anio)
        {
            return await _servicioFacturas.ObtenerReporteAnualAsync(anio);
        }

        public async Task<List<Factura>> ObtenerFacturasPorAnioAsync(int anio)
        {
            return await _servicioFacturas.ObtenerFacturasPorAnioAsync(anio);
        }

        public async Task<DatosDashboard> ObtenerDashboardAsync() 
        {
            return await _servicioFacturas.ObtenerDashboardAsync(); 
        }

        public async Task CambiarEstadoArchivoAsync(int id, bool archivar)
        {
            await _servicioFacturas.CambiarEstadoArchivoAsync(id, archivar);
        }

        public async Task<List<Factura>> ObtenerFacturasArchivadasAsync()
        {
            return await _servicioFacturas.ObtenerFacturasArchivadasAsync();
        }
    }
}
