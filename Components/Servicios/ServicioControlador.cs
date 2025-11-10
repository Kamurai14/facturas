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

        public async Task<Factura> ObtenerFacturasCompletaAsync(int facturaID) 
        {
            return await _servicioFacturas.ObtenerFacturaCompletaAsync(facturaID);
        }
    }
}
