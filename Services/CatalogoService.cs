using Npgsql;
using MomosAPI.Models;

namespace MomosAPI.Services;

public class CatalogoService(DbConnectionFactory db, ConfiguracionService cfg, IHttpContextAccessor http)
{
    private string BaseUrl()
    {
        var req = http.HttpContext?.Request;
        return req == null ? "" : $"{req.Scheme}://{req.Host}";
    }

    public List<ProductoDto> ObtenerProductos()
    {
        int max = int.TryParse(cfg.ObtenerValor("APIMovilMaxProductos"), out var m) ? m : 5000;
        var baseUrl = BaseUrl();
        var list    = new List<ProductoDto>();
        using var conn = db.Create();
        const string sql = @"
            SELECT p.Id, COALESCE(p.CodigoBarras,''), p.Nombre, COALESCE(p.Descripcion,''),
                   COALESCE(c.Nombre,''), COALESCE(u.Nombre,''),
                   p.PrecioCompra, p.PrecioVenta, p.PrecioMayoreo, p.CantidadMayoreo,
                   p.Descuento, p.StockActual, p.EsServicio, p.PrecioFijo,
                   COALESCE(p.RutaImagen,''), COALESCE(p.ClaveProducto,'')
            FROM Productos p
            LEFT JOIN Categorias c      ON c.Id = p.CategoriaId
            LEFT JOIN UnidadesMedida u  ON u.Id = p.UnidadMedidaId
            WHERE p.Activo = true LIMIT @lim";
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("lim", max);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var ruta = r.GetString(14);
            var url  = string.IsNullOrEmpty(ruta) ? "" : $"{baseUrl}/imagenes/{System.IO.Path.GetFileName(ruta)}";
            list.Add(new ProductoDto
            {
                Id             = r.GetInt32(0),
                CodigoBarras   = r.GetString(1),
                Nombre         = r.GetString(2),
                Descripcion    = r.GetString(3),
                Categoria      = r.GetString(4),
                UnidadMedida   = r.GetString(5),
                PrecioCompra   = r.GetDecimal(6),
                PrecioVenta    = r.GetDecimal(7),
                PrecioMayoreo  = r.GetDecimal(8),
                CantidadMayoreo= r.GetDecimal(9),
                Descuento      = r.GetDecimal(10),
                StockActual    = r.GetDecimal(11),
                EsServicio     = r.GetBoolean(12),
                PrecioFijo     = r.GetBoolean(13),
                UrlImagen      = url,
                ClaveProducto  = r.GetString(15)
            });
        }
        return list;
    }

    public List<PromocionDto> ObtenerPromociones()
    {
        var list = new List<PromocionDto>();
        using var conn = db.Create();
        const string sql = @"
            SELECT Id,ProductoId,Nombre,Tipo,CantidadRequerida,CantidadRegalo,
                   DescuentoPorcentaje,AplicaTotalVenta,MontoMinimoVenta,FechaInicio,FechaFin
            FROM Promociones WHERE Activo=true AND FechaInicio<=NOW() AND FechaFin>=NOW()";
        using var cmd = new NpgsqlCommand(sql, conn);
        using var r   = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new PromocionDto
            {
                Id                  = r.GetInt32(0),
                ProductoId          = r.IsDBNull(1) ? null : r.GetInt32(1),
                Nombre              = r.GetString(2),
                Tipo                = r.GetString(3),
                CantidadRequerida   = r.IsDBNull(4) ? null : r.GetDecimal(4),
                CantidadRegalo      = r.IsDBNull(5) ? null : r.GetDecimal(5),
                DescuentoPorcentaje = r.IsDBNull(6) ? null : r.GetDecimal(6),
                AplicaTotalVenta    = r.GetBoolean(7),
                MontoMinimoVenta    = r.IsDBNull(8) ? null : r.GetDecimal(8),
                FechaInicio         = r.GetDateTime(9),
                FechaFin            = r.GetDateTime(10)
            });
        return list;
    }

    public List<ClienteDto> ObtenerClientes()
    {
        var list = new List<ClienteDto>();
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand(
            "SELECT Id,Nombre,COALESCE(Telefono,''),COALESCE(Correo,''),LimiteCredito,Saldo FROM Clientes WHERE Estado='ACTIVO' ORDER BY Nombre", conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ClienteDto
            {
                Id           = r.GetInt32(0),
                Nombre       = r.GetString(1),
                Telefono     = r.GetString(2),
                Correo       = r.GetString(3),
                LimiteCredito= r.GetDecimal(4),
                Saldo        = r.GetDecimal(5)
            });
        return list;
    }

    public Dictionary<string, string> ObtenerConfiguracion()
    {
        var claves = new[] { "NombreNegocio","RFC","Direccion","MensajeTicket","GiroPrincipal","GiroFarmaceutico" };
        var dic = new Dictionary<string, string>();
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand("SELECT Clave,Valor FROM Configuracion WHERE Clave=ANY(@c)", conn);
        cmd.Parameters.AddWithValue("c", claves);
        using var r = cmd.ExecuteReader();
        while (r.Read()) dic[r.GetString(0)] = r.GetString(1);
        return dic;
    }

    public List<CategoriaDto> ObtenerCategorias()
    {
        var list = new List<CategoriaDto>();
        using var conn = db.Create();
        using var cmd  = new NpgsqlCommand("SELECT Id,Nombre FROM Categorias ORDER BY Nombre", conn);
        using var r    = cmd.ExecuteReader();
        while (r.Read()) list.Add(new CategoriaDto { Id = r.GetInt32(0), Nombre = r.GetString(1) });
        return list;
    }
}
