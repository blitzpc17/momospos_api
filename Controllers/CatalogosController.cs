using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomosAPI.Services;

namespace MomosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CatalogosController(CatalogoService catalogoSvc, ConfiguracionService cfgSvc) : ControllerBase
{
    [HttpGet("productos")]
    public IActionResult GetProductos()
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva")) return StatusCode(503);
        return Ok(catalogoSvc.ObtenerProductos());
    }

    [HttpGet("promociones")]
    public IActionResult GetPromociones()
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva")) return StatusCode(503);
        return Ok(catalogoSvc.ObtenerPromociones());
    }

    [HttpGet("clientes")]
    public IActionResult GetClientes()
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva")) return StatusCode(503);
        return Ok(catalogoSvc.ObtenerClientes());
    }

    [HttpGet("configuracion")]
    public IActionResult GetConfiguracion()
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva")) return StatusCode(503);
        return Ok(catalogoSvc.ObtenerConfiguracion());
    }

    [HttpGet("categorias")]
    public IActionResult GetCategorias()
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva")) return StatusCode(503);
        return Ok(catalogoSvc.ObtenerCategorias());
    }
}
