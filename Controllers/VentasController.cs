using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomosAPI.Models;
using MomosAPI.Services;
using System.Security.Claims;

namespace MomosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VentasController(
    VentaService ventaSvc,
    DispositivoService dispositivoSvc,
    ConfiguracionService cfgSvc) : ControllerBase
{
    private int GetDispositivoId() =>
        int.Parse(User.FindFirstValue("dispositivoId") ?? "0");

    [HttpGet("folio-siguiente")]
    public IActionResult GetFolioSiguiente([FromQuery] string tipo = "VENTA")
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva"))    return StatusCode(503);
        if (!cfgSvc.EsVerdadero("APIMovilVentasActivas")) return StatusCode(503, new { mensaje = "Ventas móviles desactivadas." });

        int did = GetDispositivoId();
        if (did == 0) return Unauthorized();
        return Ok(dispositivoSvc.SiguienteFolio(did, tipo));
    }

    [HttpPost]
    public IActionResult PostVenta([FromBody] VentaRequest req)
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva"))    return StatusCode(503);
        if (!cfgSvc.EsVerdadero("APIMovilVentasActivas")) return StatusCode(503, new { mensaje = "Ventas móviles desactivadas." });

        int did = GetDispositivoId();
        if (did == 0) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.Folio))
            return BadRequest(new { mensaje = "El folio es obligatorio." });
        if (req.Detalles == null || req.Detalles.Count == 0)
            return BadRequest(new { mensaje = "La venta debe tener al menos un detalle." });

        var result = ventaSvc.RegistrarVenta(req, did);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
