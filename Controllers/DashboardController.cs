using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomosAPI.Models;
using System.Data;
using Dapper;
using Npgsql;

namespace MomosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(IConfiguration config) : ControllerBase
{
    private string GetConnectionString()
    {
        return config.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpGet("metricas")]
    public IActionResult GetMetricas([FromQuery] DateTime? inicio, [FromQuery] DateTime? fin)
    {
        try
        {
            DateTime inicioDia = (inicio ?? DateTime.Today).Date;
            DateTime finDia = (fin ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            var metricas = new DashboardMetrics();
            using (IDbConnection db = new NpgsqlConnection(GetConnectionString()))
            {
                // Ventas y Tickets de Hoy
                string sqlVentas = @"
                    SELECT COALESCE(SUM(Total), 0) AS VentasHoy, COUNT(Id) AS TicketsHoy
                    FROM Ventas 
                    WHERE Fecha BETWEEN @Inicio AND @Fin AND Estado = 'CONFIRMADO';";
                var ventasResult = db.QuerySingle(sqlVentas, new { Inicio = inicioDia, Fin = finDia });
                metricas.VentasHoy = Convert.ToDecimal(ventasResult.ventashoy);
                metricas.TicketsHoy = Convert.ToInt32(ventasResult.ticketshoy);

                // Cuentas por Cobrar (Suma del saldo de clientes)
                string sqlCuentas = "SELECT COALESCE(SUM(Saldo), 0) FROM Clientes WHERE Saldo > 0;";
                metricas.CuentasPorCobrar = db.ExecuteScalar<decimal>(sqlCuentas);

                // Productos Críticos (Sin stock o Bajo stock)
                string sqlCriticos = "SELECT COUNT(Id) FROM Productos WHERE EsServicio = FALSE AND StockActual <= StockMinimo;";
                metricas.ProductosCriticos = db.ExecuteScalar<int>(sqlCriticos);

                // Retiros Hoy
                string sqlRetiros = "SELECT COALESCE(SUM(Importe), 0) FROM CajaMovimientos WHERE Tipo = 'RETIRO' AND Fecha BETWEEN @Inicio AND @Fin;";
                metricas.RetirosHoy = db.ExecuteScalar<decimal>(sqlRetiros, new { Inicio = inicioDia, Fin = finDia });

                // Productos Más Vendidos
                string sqlMasVendidos = @"
                    SELECT p.CodigoBarras, p.Nombre, SUM(vd.Cantidad) as CantidadTotal, SUM(vd.Subtotal) as TotalGenerado
                    FROM VentaDetalles vd
                    INNER JOIN Ventas v ON vd.VentaId = v.Id
                    INNER JOIN Productos p ON vd.ProductoId = p.Id
                    WHERE v.Fecha BETWEEN @Inicio AND @Fin AND v.Estado = 'CONFIRMADO'
                    GROUP BY p.CodigoBarras, p.Nombre
                    ORDER BY CantidadTotal DESC LIMIT 5;";
                metricas.ProductosMasVendidos = db.Query<ArticuloVendidoDTO>(sqlMasVendidos, new { Inicio = inicioDia, Fin = finDia }).AsList();

                // Productos Menos Vendidos
                string sqlMenosVendidos = @"
                    SELECT p.CodigoBarras, p.Nombre, SUM(vd.Cantidad) as CantidadTotal, SUM(vd.Subtotal) as TotalGenerado
                    FROM VentaDetalles vd
                    INNER JOIN Ventas v ON vd.VentaId = v.Id
                    INNER JOIN Productos p ON vd.ProductoId = p.Id
                    WHERE v.Fecha BETWEEN @Inicio AND @Fin AND v.Estado = 'CONFIRMADO'
                    GROUP BY p.CodigoBarras, p.Nombre
                    ORDER BY CantidadTotal ASC LIMIT 5;";
                metricas.ProductosMenosVendidos = db.Query<ArticuloVendidoDTO>(sqlMenosVendidos, new { Inicio = inicioDia, Fin = finDia }).AsList();

                // Productos con Stock Bajo
                string sqlStockBajo = @"
                    SELECT CodigoBarras, Nombre, StockActual, StockMinimo 
                    FROM Productos 
                    WHERE EsServicio = FALSE AND StockActual <= StockMinimo 
                    ORDER BY StockActual ASC;";
                metricas.ProductosStockBajo = db.Query<ReporteExistenciasDTO>(sqlStockBajo).AsList();

                // Lotes Próximos a Caducar (en los próximos 90 días)
                string sqlCaducidad = @"
                    SELECT 
                        p.CodigoBarras, 
                        p.Nombre as ProductoNombre, 
                        pl.NumeroLote, 
                        pl.StockActual as StockLote, 
                        pl.FechaCaducidad,
                        EXTRACT(DAY FROM (pl.FechaCaducidad - CURRENT_TIMESTAMP)) as DiasRestantes
                    FROM ProductoLotes pl
                    INNER JOIN Productos p ON pl.ProductoId = p.Id
                    WHERE pl.StockActual > 0 
                      AND pl.FechaCaducidad <= CURRENT_TIMESTAMP + INTERVAL '90 days'
                    ORDER BY pl.FechaCaducidad ASC;";
                metricas.LotesProximosCaducar = db.Query<LoteCaducidadDTO>(sqlCaducidad).AsList();
            }
            return Ok(metricas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al obtener métricas", error = ex.Message });
        }
    }
}
