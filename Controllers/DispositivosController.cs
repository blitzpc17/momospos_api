using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomosAPI.Models;
using MomosAPI.Services;

namespace MomosAPI.Controllers;

/// <summary>
/// Administración de dispositivos móviles (requiere rol Administrador via JWT con claim admin).
/// Por simplicidad inicial se deja sin restricción de rol, se puede añadir [Authorize(Roles="Admin")] cuando se implemente en Flutter.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DispositivosController(DispositivoService dispositivoSvc) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(dispositivoSvc.Listar());

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var d = dispositivoSvc.ObtenerPorId(id);
        return d == null ? NotFound() : Ok(d);
    }

    [HttpPost]
    public IActionResult Crear([FromBody] CrearDispositivoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre))
            return BadRequest(new { mensaje = "El nombre es obligatorio." });
        var d = dispositivoSvc.Crear(req);
        return CreatedAtAction(nameof(GetById), new { id = d.Id }, d);
    }

    [HttpPut("{id:int}")]
    public IActionResult Actualizar(int id, [FromBody] CrearDispositivoRequest req)
    {
        if (!dispositivoSvc.Actualizar(id, req)) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Desactivar(int id)
    {
        if (!dispositivoSvc.ToggleActivo(id, false)) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id:int}/activar")]
    public IActionResult Activar(int id)
    {
        if (!dispositivoSvc.ToggleActivo(id, true)) return NotFound();
        return NoContent();
    }
}
