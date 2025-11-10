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
    }
}
