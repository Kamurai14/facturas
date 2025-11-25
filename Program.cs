using facturas.Components;
using facturas.Components.Data;
using facturas.Components.Servicios;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

String ruta = "facturasdb.db";

builder.Configuration.AddInMemoryCollection(new[] {
    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", $"DataSource={ruta}")
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ServicioFacturas>();
builder.Services.AddSingleton<ServicioControlador>();

var app = builder.Build();

using var conexion = new SqliteConnection(app.Configuration.GetConnectionString("DefaultConnection"));
conexion.Open();

var comando = conexion.CreateCommand();
comando.CommandText = @"
CREATE TABLE IF NOT EXISTS Facturas (
    FacturaID INTEGER PRIMARY KEY AUTOINCREMENT,
    Fecha TEXT NOT NULL,
    NombreCliente TEXT NOT NULL,
    TotalFactura DECIMAL NOT NULL
);";
comando.ExecuteNonQuery();

comando.CommandText = @"
CREATE TABLE IF NOT EXISTS FacturaProductos (
    ProductoID INTEGER PRIMARY KEY AUTOINCREMENT,
    FacturaID INTEGER NOT NULL,
    Nombre TEXT NOT NULL,
    Cantidad INTEGER NOT NULL,
    PrecioUnitario DECIMAL NOT NULL,
    FOREIGN KEY (FacturaID) REFERENCES Facturas (FacturaID) ON DELETE CASCADE
);";
comando.ExecuteNonQuery();

/*using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection");

    using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
    {
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "ALTER TABLE Facturas ADD COLUMN Archivada INTEGER DEFAULT 0;";

        try
        {
            command.ExecuteNonQuery();
            Console.WriteLine("¡Base de datos actualizada con éxito!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Nota: " + ex.Message);
        }
    }
}*/

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
