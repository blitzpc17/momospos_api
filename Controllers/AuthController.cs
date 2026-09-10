using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MomosAPI.Models;
using MomosAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MomosAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    DispositivoService dispositivoSvc,
    ConfiguracionService cfgSvc,
    IConfiguration config) : ControllerBase
{
    [HttpPost("login-device")]
    public IActionResult LoginDevice([FromBody] LoginDispositivoRequest req)
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva"))
            return StatusCode(503, new { mensaje = "API móvil desactivada en configuración." });

        var dispositivo = dispositivoSvc.ObtenerPorToken(req.Token);
        if (dispositivo == null || !dispositivo.Activo)
            return Unauthorized(new { mensaje = "Token de dispositivo inválido o inactivo." });

        dispositivoSvc.ActualizarUltimaConexion(dispositivo.Id);

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        int expHoras = int.TryParse(cfgSvc.ObtenerValor("APIMovilJwtExpHoras"), out var h) ? h : 24;

        var claims = new[]
        {
            new Claim("dispositivoId", dispositivo.Id.ToString()),
            new Claim("dispositivoNombre", dispositivo.Nombre),
            new Claim(ClaimTypes.Role, "Dispositivo")
        };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(expHoras),
            signingCredentials: creds,
            claims: claims);

        return Ok(new LoginDispositivoResponse(
            Jwt: new JwtSecurityTokenHandler().WriteToken(token),
            DispositivoNombre: dispositivo.Nombre,
            DispositivoId: dispositivo.Id));
    }
}
