namespace MomosAPI.Models;

public record LoginDispositivoRequest(string Token);
public record LoginDispositivoResponse(string Jwt, string DispositivoNombre, int DispositivoId);

public class ProductoDto
{
    public int     Id              { get; set; }
    public string  CodigoBarras    { get; set; } = "";
    public string  Nombre          { get; set; } = "";
    public string  Descripcion     { get; set; } = "";
    public string  Categoria       { get; set; } = "";
    public string  UnidadMedida    { get; set; } = "";
    public decimal PrecioCompra    { get; set; }
    public decimal PrecioVenta     { get; set; }
    public decimal PrecioMayoreo   { get; set; }
    public decimal CantidadMayoreo { get; set; }
    public decimal Descuento       { get; set; }
    public decimal StockActual     { get; set; }
    public bool    EsServicio      { get; set; }
    public bool    PrecioFijo      { get; set; }
    public string  UrlImagen       { get; set; } = "";
    public string  ClaveProducto   { get; set; } = "";
}

public class PromocionDto
{
    public int      Id                  { get; set; }
    public int?     ProductoId          { get; set; }
    public string   Nombre              { get; set; } = "";
    public string   Tipo                { get; set; } = "";
    public decimal? CantidadRequerida   { get; set; }
    public decimal? CantidadRegalo      { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
    public bool     AplicaTotalVenta    { get; set; }
    public decimal? MontoMinimoVenta    { get; set; }
    public DateTime FechaInicio         { get; set; }
    public DateTime FechaFin            { get; set; }
}

public class ClienteDto
{
    public int     Id            { get; set; }
    public string  Nombre        { get; set; } = "";
    public string  Telefono      { get; set; } = "";
    public string  Correo        { get; set; } = "";
    public decimal LimiteCredito { get; set; }
    public decimal Saldo         { get; set; }
}

public class CategoriaDto { public int Id { get; set; } public string Nombre { get; set; } = ""; }

public class VentaRequest
{
    public string  Folio          { get; set; } = "";
    public int?    ClienteId      { get; set; }
    public decimal Total          { get; set; }
    public decimal Pagado         { get; set; }
    public decimal Cambio         { get; set; }
    public decimal DescuentoTotal { get; set; }
    public List<VentaDetalleRequest> Detalles { get; set; } = [];
    public List<VentaPagoRequest>    Pagos    { get; set; } = [];
}

public class VentaDetalleRequest
{
    public int     ProductoId      { get; set; }
    public string  Descripcion     { get; set; } = "";
    public decimal Cantidad        { get; set; }
    public decimal PrecioUnitario  { get; set; }
    public decimal Subtotal        { get; set; }
    public decimal DescuentoManual { get; set; }
}

public class VentaPagoRequest  { public string MetodoPago { get; set; } = "EFECTIVO"; public decimal Importe { get; set; } }
public class VentaResponse     { public bool Success { get; set; } public int VentaId { get; set; } public string Folio { get; set; } = ""; public string Mensaje { get; set; } = ""; }
public class FolioResponse     { public string Folio { get; set; } = ""; public long Consecutivo { get; set; } }

public class DispositivoDto
{
    public int       Id              { get; set; }
    public string    Nombre          { get; set; } = "";
    public string    Token           { get; set; } = "";
    public bool      Activo          { get; set; }
    public string    PrefixFolio     { get; set; } = "MOV";
    public DateTime  CreadoEn        { get; set; }
    public DateTime? UltimaConexion  { get; set; }
}

public class CrearDispositivoRequest { public string Nombre { get; set; } = ""; public string PrefixFolio { get; set; } = "MOV"; }

public record LoginUsuarioRequest(string UsuarioLogin, string Password, string DeviceId, string DeviceName);
public record LoginUsuarioResponse(string Jwt, string UsuarioNombre, int UsuarioId, string DispositivoNombre, int DispositivoId);

public class UsuarioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string UsuarioLogin { get; set; } = "";
    public bool EsAdmin { get; set; }
    public string Estado { get; set; } = "";
}


public class DashboardMetrics
{
    public decimal VentasHoy { get; set; }
    public int TicketsHoy { get; set; }
    public decimal CuentasPorCobrar { get; set; }
    public int ProductosCriticos { get; set; }
    public decimal RetirosHoy { get; set; }
    public List<ArticuloVendidoDTO> ProductosMasVendidos { get; set; } = new();
    public List<ArticuloVendidoDTO> ProductosMenosVendidos { get; set; } = new();
    public List<ReporteExistenciasDTO> ProductosStockBajo { get; set; } = new();
    public List<LoteCaducidadDTO> LotesProximosCaducar { get; set; } = new();
}

public class ArticuloVendidoDTO
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadTotal { get; set; }
    public decimal TotalGenerado { get; set; }
}

public class ReporteExistenciasDTO
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
}

public class LoteCaducidadDTO
{
    public string CodigoBarras { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public decimal StockLote { get; set; }
    public DateTime FechaCaducidad { get; set; }
    public int DiasRestantes { get; set; }
}
