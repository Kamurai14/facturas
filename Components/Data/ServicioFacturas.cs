using Microsoft.Data.Sqlite;

namespace facturas.Components.Data
{
    public class ServicioFacturas
    {
        private readonly string _connectionString;

        
        public ServicioFacturas(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        
        public async Task GuardarFacturaAsync(Factura factura)
        {
            using var conexion = new SqliteConnection(_connectionString);
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();

            try
            {
        
                var comandoFactura = conexion.CreateCommand();
                comandoFactura.Transaction = transaccion;
                comandoFactura.CommandText =
                    @"INSERT INTO Facturas (Fecha, NombreCliente, TotalFactura)
                      VALUES ($fecha, $nombre, $total);
                      SELECT last_insert_rowid();"; 

                comandoFactura.Parameters.AddWithValue("$fecha", factura.Fecha.ToString("o"));
                comandoFactura.Parameters.AddWithValue("$nombre", factura.Nombre);
                comandoFactura.Parameters.AddWithValue("$total", factura.TotalFactura);

                long nuevaFacturaID = (long)await comandoFactura.ExecuteScalarAsync();

              
                foreach (var producto in factura.Productos)
                {
                    var comandoProducto = conexion.CreateCommand();
                    comandoProducto.Transaction = transaccion;
                    comandoProducto.CommandText =
                        @"INSERT INTO FacturaProductos (FacturaID, Nombre, Cantidad, PrecioUnitario)
                          VALUES ($facturaID, $nombreProd, $cantidad, $precio)";

                    comandoProducto.Parameters.AddWithValue("$facturaID", nuevaFacturaID);
                    comandoProducto.Parameters.AddWithValue("$nombreProd", producto.Nombre);
                    comandoProducto.Parameters.AddWithValue("$cantidad", producto.Cantidad);
                    comandoProducto.Parameters.AddWithValue("$precio", producto.PrecioUnitario);

                    await comandoProducto.ExecuteNonQueryAsync();
                }

               
                await transaccion.CommitAsync();
            }
            catch (Exception)
            {
              
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Factura>> ObtenerFacturasAsync()
        {
            var facturas = new List<Factura>();
            using var conexion = new SqliteConnection(_connectionString);
            await conexion.OpenAsync();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"
        SELECT FacturaID, Fecha, NombreCliente, TotalFactura 
        FROM Facturas 
        ORDER BY FacturaID DESC";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(0),
                    Fecha = DateOnly.Parse(lector.GetString(1)),
                    Nombre = lector.GetString(2),
                    TotalFactura = lector.GetDecimal(3)
                });
            }
            return facturas;
        }

        public async Task<Factura> ObtenerFacturaCompletaAsync(int facturaID) 
        {
            using var conexion = new SqliteConnection(_connectionString);
            await conexion.OpenAsync();

            var factura = new Factura();
            var comandoFactura = conexion.CreateCommand();
            comandoFactura.CommandText = @"SELECT * FROM Facturas WHERE FacturaID = $id";
            comandoFactura.Parameters.AddWithValue("$id", facturaID);

            using (var lector = await comandoFactura.ExecuteReaderAsync()) 
            {
                factura.FacturaID = lector.GetInt32(0);
                factura.Fecha = DateOnly.Parse(lector.GetString(1));
                factura.Nombre = lector.GetString(2);
                factura.TotalFactura = lector.GetDecimal(3);
            }

            var comandoProductos = conexion.CreateCommand();
            comandoProductos.CommandText = @"SELECT Nombre,Cantidad,PrecioUnitario FROM Facturas WHERE FacturaID = $id";
            comandoProductos.Parameters.AddWithValue("$id", facturaID);

            using (var lector = await comandoProductos.ExecuteReaderAsync()) 
            {
                while (await lector.ReadAsync()) 
                {
                    factura.Productos.Add(new Producto
                    {
                        Nombre = lector.GetString(0),
                        Cantidad = lector.GetInt32(1),
                        PrecioUnitario = lector.GetDecimal(2)
                    });                                   
                }
            }
            return factura;
        }

        public async Task ActualizarFacturaAsync(Factura factura) 
        {
            using var conexion = new SqliteConnection(_connectionString);
            await conexion.OpenAsync();
            using var transaccion = conexion.BeginTransaction();

            try
            {
                var comandoFactura = conexion.CreateCommand();
                comandoFactura.Transaction = transaccion;
                comandoFactura.CommandText =
                    @"UPDATE Facturas
                      SET Fecha = $fecha, NombreCliente = $nombre, TotalFactura = $total
                      WHERE FacturaID = $id";

                comandoFactura.Parameters.AddWithValue("$fecha", factura.Fecha.ToString("o"));
                comandoFactura.Parameters.AddWithValue("$nombre", factura.Nombre);
                comandoFactura.Parameters.AddWithValue("$total", factura.TotalFactura);
                comandoFactura.Parameters.AddWithValue("$id", factura.FacturaID);
                await comandoFactura.ExecuteNonQueryAsync();

                var comandoBorrarProductos = conexion.CreateCommand();
                comandoBorrarProductos.Transaction = transaccion;
                comandoBorrarProductos.CommandText = @"DELETE FROM FacturaProductos WHERE FacturaID = $id";
                comandoBorrarProductos.Parameters.AddWithValue("$id", factura.FacturaID);
                await comandoBorrarProductos.ExecuteNonQueryAsync();

                foreach (var productos in factura.Productos)
                {
                    var comandoProducto = conexion.CreateCommand();
                    comandoProducto.Transaction = transaccion;
                    comandoProducto.CommandText =
                        @"INSERT INTO FacturaProductos (FacturaID, Nombre, Cantidad, PrecioUnitario)
                          VALUES ($facturaID, $nombreProd, $cantidad, $precio";

                    comandoProducto.Parameters.AddWithValue("$facturaID", factura.FacturaID);
                    comandoProducto.Parameters.AddWithValue("$nombreProd", productos.Nombre);
                    comandoProducto.Parameters.AddWithValue("$cantidad", productos.Cantidad);
                    comandoProducto.Parameters.AddWithValue("$precio", productos.PrecioUnitario);
                    await comandoProducto.ExecuteNonQueryAsync();
                }
                await transaccion.CommitAsync();
            }
            catch 
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }
    }
}
