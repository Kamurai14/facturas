using Microsoft.Data.Sqlite;
using System.Globalization;

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
            await using var conexion = await ObtenerConexionAbiertaAsync();
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
                await InsertarProductosAsync(transaccion, factura.Productos, nuevaFacturaID);
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
            await using var conexion = await ObtenerConexionAbiertaAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
        SELECT FacturaID, Fecha, NombreCliente, TotalFactura 
        FROM Facturas 
        WHERE Archivada = 0
        ORDER BY FacturaID DESC";

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(lector.GetOrdinal("FacturaID")),
                    Fecha = DateOnly.Parse(lector.GetString(lector.GetOrdinal("Fecha"))),
                    Nombre = lector.GetString(lector.GetOrdinal("NombreCliente")),
                    TotalFactura = lector.GetDecimal(lector.GetOrdinal("TotalFactura"))
                });
            }
            return facturas;
        }

        public async Task<Factura> ObtenerFacturaCompletaAsync(int facturaID) 
        {
            Factura? factura = null;
            await using var conexion = await ObtenerConexionAbiertaAsync();

            var comandoFactura = conexion.CreateCommand();
            comandoFactura.CommandText = @"SELECT FacturaID, Fecha, NombreCliente, TotalFactura FROM Facturas WHERE FacturaID = $id";
            comandoFactura.Parameters.AddWithValue("$id", facturaID);

            using (var lector = await comandoFactura.ExecuteReaderAsync()) 
            {
                if (await lector.ReadAsync()) 
                {
                    factura = new Factura
                    {
                        FacturaID = lector.GetInt32(lector.GetOrdinal("FacturaID")),
                        Fecha = DateOnly.Parse(lector.GetString(lector.GetOrdinal("Fecha"))),
                        Nombre = lector.GetString(lector.GetOrdinal("NombreCliente")),
                        TotalFactura = lector.GetDecimal(lector.GetOrdinal("TotalFactura"))
                    };
                }
                
            }
            if (factura != null)
            {
                var comandoProductos = conexion.CreateCommand();
                comandoProductos.CommandText = @"SELECT Nombre, Cantidad, PrecioUnitario FROM FacturaProductos WHERE FacturaID = $id";
                comandoProductos.Parameters.AddWithValue("$id", facturaID);

                using (var lector = await comandoProductos.ExecuteReaderAsync()) 
                {
                    while (await lector.ReadAsync()) 
                    {
                        factura.Productos.Add(new Producto
                        {
                            Nombre = lector.GetString(lector.GetOrdinal("Nombre")),
                            Cantidad = lector.GetInt32(lector.GetOrdinal("Cantidad")),
                            PrecioUnitario = lector.GetDecimal(lector.GetOrdinal("PrecioUnitario"))
                        });
                    }
                }
            }
            return factura;
        }

        public async Task ActualizarFacturaAsync(Factura factura)
        {
            await using var conexion = await ObtenerConexionAbiertaAsync();
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

                var comandoBorrarProds = conexion.CreateCommand();
                comandoBorrarProds.Transaction = transaccion;

                comandoBorrarProds.CommandText = @"DELETE FROM FacturaProductos WHERE FacturaID = $id";

                comandoBorrarProds.Parameters.AddWithValue("$id", factura.FacturaID);
                await comandoBorrarProds.ExecuteNonQueryAsync();

                await InsertarProductosAsync(transaccion, factura.Productos, factura.FacturaID);
                await transaccion.CommitAsync();
            }
            catch (Exception)
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        public async Task EliminarFacturaAsync(int facturaID) 
        {
            await using var conexion = await ObtenerConexionAbiertaAsync();

            var comando = conexion.CreateCommand();
            comando.CommandText = @"DELETE FROM Facturas WHERE FacturaID = $id";
            comando.Parameters.AddWithValue("$id", facturaID);
            await comando.ExecuteNonQueryAsync();
        }

        private async Task<SqliteConnection> ObtenerConexionAbiertaAsync()
        {
            var conexion = new SqliteConnection(_connectionString);
            await conexion.OpenAsync();
            return conexion;
        }

        private async Task InsertarProductosAsync(SqliteTransaction transaccion, List<Producto> productos, long facturaID) 
        {
            foreach (var producto in productos) 
            {
                var comandoProducto = transaccion.Connection!.CreateCommand();
                comandoProducto.Transaction = transaccion;

                comandoProducto.CommandText = @"INSERT INTO FacturaProductos (FacturaID, Nombre, Cantidad, PrecioUnitario) 
                                                VALUES ($facturaID, $nombreProd, $cantidad, $precio)";

                comandoProducto.Parameters.AddWithValue("$facturaID", facturaID);
                comandoProducto.Parameters.AddWithValue("$nombreProd", producto.Nombre);
                comandoProducto.Parameters.AddWithValue("$cantidad", producto.Cantidad);
                comandoProducto.Parameters.AddWithValue("$precio", producto.PrecioUnitario);

                await comandoProducto.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<ReporteMensual>> ObtenerReporteAnualAsync(int anio)
        {
            var reporte = new List<ReporteMensual>();
            await using var conexion = await ObtenerConexionAbiertaAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
        SELECT 
            STRFTIME('%m', Fecha) AS Mes, 
            SUM(TotalFactura) AS TotalMes
        FROM Facturas
        WHERE STRFTIME('%Y', Fecha) = $anio AND Archivada = 0
        GROUP BY Mes
        ORDER BY Mes ASC;";

            comando.Parameters.AddWithValue("$anio", anio.ToString());

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                reporte.Add(new ReporteMensual
                {
                    Mes = lector.GetString(lector.GetOrdinal("Mes")),
                    TotalMes = lector.GetDecimal(lector.GetOrdinal("TotalMes"))
                });
            }
            return reporte;
        }

        public async Task<List<Factura>> ObtenerFacturasPorAnioAsync(int anio)
        {
            var facturas = new List<Factura>();
            await using var conexion = await ObtenerConexionAbiertaAsync();
            var comando = conexion.CreateCommand();
            comando.CommandText = @"
        SELECT FacturaID, Fecha, NombreCliente, TotalFactura 
        FROM Facturas 
        WHERE STRFTIME('%Y', Fecha) = $anio AND Archivada = 0
        ORDER BY Fecha ASC";

            comando.Parameters.AddWithValue("$anio", anio.ToString());

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(lector.GetOrdinal("FacturaID")),
                    Fecha = DateOnly.Parse(lector.GetString(lector.GetOrdinal("Fecha"))),
                    Nombre = lector.GetString(lector.GetOrdinal("NombreCliente")),
                    TotalFactura = lector.GetDecimal(lector.GetOrdinal("TotalFactura"))
                });
            }
            return facturas;
        }

        public async Task<DatosDashboard> ObtenerDashboardAsync()
        {
            var datos = new DatosDashboard();
            await using var conexion = await ObtenerConexionAbiertaAsync();

            var cmdCliente = conexion.CreateCommand();
            cmdCliente.CommandText = @"
                SELECT NombreCliente, SUM(TotalFactura) as Total
                FROM Facturas
                WHERE Archivada = 0
                GROUP BY NombreCliente
                ORDER BY Total DESC
                LIMIT 1";

            using (var lector = await cmdCliente.ExecuteReaderAsync())
            {
                if (await lector.ReadAsync())
                {
                    datos.TopClienteNombre = lector.GetString(0);
                    datos.TopClienteTotal = lector.GetDecimal(1);
                }
            }

            var cmdMes = conexion.CreateCommand();
            cmdMes.CommandText = @"
                SELECT STRFTIME('%m', Fecha) as Mes, SUM(TotalFactura) as Total
                FROM Facturas
                WHERE Archivada = 0 
                GROUP BY Mes
                ORDER BY Total DESC
                LIMIT 1";

            using (var lector = await cmdMes.ExecuteReaderAsync())
            {
                if (await lector.ReadAsync())
                {
                    datos.TopMesNumero = lector.GetString(0);
                    datos.TopMesTotal = lector.GetDecimal(1);   
                }
            }

            var cmdProd = conexion.CreateCommand();
            cmdProd.CommandText = @"
                SELECT FP.Nombre, SUM(FP.Cantidad) as TotalVendidos
                FROM FacturaProductos FP
                INNER JOIN Facturas F ON FP.FacturasID = F.FacturaID
                WHERE F.Archivada = 0 
                GROUP BY FP.Nombre
                ORDER BY TotalVendidos DESC
                LIMIT 1";

            using (var lector = await cmdProd.ExecuteReaderAsync()) 
            {
                if (await lector.ReadAsync()) 
                {
                    datos.TopProductoNombre = lector.GetString(0);  
                    datos.TopProductoCantidad = lector.GetInt32(1);
                }
            }

            var cmdPromedio = conexion.CreateCommand();
            cmdPromedio.CommandText = "SELECT AVG(TotalFactura), COUNT(*) FROM Facturas WHERE Archivada = 0";

            using (var lector = await cmdPromedio.ExecuteReaderAsync())
            {
                if (await lector.ReadAsync())
                {
                    if (!lector.IsDBNull(0))
                    {
                        datos.TicketPromedio = lector.GetDecimal(0);
                        datos.TotalFacturasEmitidas = lector.GetInt32(1);
                    }
                }
            }

            var cmdRecientes = conexion.CreateCommand();
            cmdRecientes.CommandText = @"
                SELECT FacturaID, Fecha, NombreCliente, TotalFactura 
                FROM Facturas 
                WHERE Archivada = 0
                ORDER BY FacturaID DESC 
                LIMIT 5"; 

            using (var lector = await cmdRecientes.ExecuteReaderAsync())
            {
                while (await lector.ReadAsync())
                {
                    datos.UltimasFacturas.Add(new Factura
                    {
                        FacturaID = lector.GetInt32(0),
                        Fecha = DateOnly.Parse(lector.GetString(1)),
                        Nombre = lector.GetString(2),
                        TotalFactura = lector.GetDecimal(3)
                    });
                }
            }

            var cmdMesActual = conexion.CreateCommand();
            cmdMesActual.CommandText = @"
                SELECT SUM(TotalFactura) 
                FROM Facturas 
                WHERE STRFTIME('%Y-%m', Fecha) = STRFTIME('%Y-%m', 'now')
                AND Archivada = 0";

            var resultadoMes = await cmdMesActual.ExecuteScalarAsync();
            if (resultadoMes != DBNull.Value && resultadoMes != null)
            {
                datos.VentasMesActual = Convert.ToDecimal(resultadoMes);
            }

            var cmdClientes = conexion.CreateCommand();
            cmdClientes.CommandText = "SELECT COUNT(DISTINCT NombreCliente) FROM Facturas WHERE Archivada = 0";
            datos.TotalClientesUnicos = Convert.ToInt32(await cmdClientes.ExecuteScalarAsync());

            var cmdRentable = conexion.CreateCommand();
            cmdRentable.CommandText = @"
                SELECT FP.Nombre, SUM(FP.Cantidad * FP.PrecioUnitario) as TotalGanado
                FROM FacturaProductos FP
                INNER JOIN Facturas F ON FP.FacturaID = F.FacturaID
                WHERE F.Archivada = 0
                GROUP BY FP.Nombre
                ORDER BY TotalGanado DESC
                LIMIT 1";

            using (var lector = await cmdRentable.ExecuteReaderAsync())
            {
                if (await lector.ReadAsync())
                {
                    datos.ProductoMasRentableNombre = lector.GetString(0);
                    datos.ProductoMasRentableTotal = lector.GetDecimal(1);
                }
            }

            return datos;
        }

        public async Task CambiarEstadoArchivoAsync(int facturaID, bool archivar) 
        {
            await using var conexion = await ObtenerConexionAbiertaAsync();
            var comando = conexion.CreateCommand();

            comando.CommandText = "UPDATE Facturas SET Archivada = $estado WHERE FacturaID = $id";
            comando.Parameters.AddWithValue("$estado", archivar ? 1 : 0);
            comando.Parameters.AddWithValue("$id", facturaID);
            await comando.ExecuteNonQueryAsync();
        }

        public async Task<List<Factura>> ObtenerFacturasArchivadasAsync()
        {
            var facturas = new List<Factura>();
            await using var conexion = await ObtenerConexionAbiertaAsync();
            var comando = conexion.CreateCommand();

            comando.CommandText = @"
                SELECT FacturaID, Fecha, NombreCliente, TotalFactura
                FROM Facturas
                WHERE Archivada = 1
                ORDER BY Fecha DESC";
                
            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                facturas.Add(new Factura
                {
                    FacturaID = lector.GetInt32(0),
                    Fecha = DateOnly.Parse(lector.GetString(1)),
                    Nombre = lector.GetString(2),
                    TotalFactura = lector.GetDecimal(3),
                    Archivada = true
                });
            }
            return facturas;    
        }
    }
}
