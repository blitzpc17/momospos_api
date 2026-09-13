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
    UsuarioService usuarioSvc,
    ConfiguracionService cfgSvc,
    IConfiguration config) : ControllerBase
{

    
    [HttpPost("login-user")]
    public IActionResult LoginUser([FromBody] LoginUsuarioRequest req)
    {
        if (!cfgSvc.EsVerdadero("APIMovilActiva"))
            return StatusCode(503, new { mensaje = "API móvil desactivada en configuración." });

        var usuario = usuarioSvc.Autenticar(req.UsuarioLogin, req.Password);
        if (usuario == null)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos, o usuario inactivo." });

        // Handle auto-device registration
        var dispositivo = dispositivoSvc.ObtenerPorToken(req.DeviceId);
        if (dispositivo == null)
        {
            // Create device automatically
            var newDeviceReq = new CrearDispositivoRequest 
            { 
                Nombre = string.IsNullOrEmpty(req.DeviceName) ? $"App-{req.UsuarioLogin}" : req.DeviceName,
                PrefixFolio = "MOV" 
            };
            dispositivo = dispositivoSvc.Crear(newDeviceReq);
            
            // Manually set its token to the provided DeviceId to match future requests
            using var conn = new Npgsql.NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand("UPDATE Dispositivos SET Token=@t, Activo=true WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("t", req.DeviceId);
            cmd.Parameters.AddWithValue("id", dispositivo.Id);
            cmd.ExecuteNonQuery();
            
            dispositivo.Token = req.DeviceId;
        }
        else if (!dispositivo.Activo)
        {
            return Unauthorized(new { mensaje = "El dispositivo está bloqueado." });
        }

        dispositivoSvc.ActualizarUltimaConexion(dispositivo.Id);

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        int expHoras = int.TryParse(cfgSvc.ObtenerValor("APIMovilJwtExpHoras"), out var h) ? h : 24;

        var claims = new[]
        {
            new Claim("usuarioId", usuario.Id.ToString()),
            new Claim("nombre", usuario.Nombre),
            new Claim("dispositivoId", dispositivo.Id.ToString()),
            new Claim("dispositivoNombre", dispositivo.Nombre),
            new Claim(ClaimTypes.Role, "Dispositivo"),
            new Claim(ClaimTypes.Role, usuario.EsAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddHours(expHoras),
            signingCredentials: creds,
            claims: claims);

        return Ok(new LoginUsuarioResponse(
            Jwt: new JwtSecurityTokenHandler().WriteToken(token),
            UsuarioNombre: usuario.Nombre,
            UsuarioId: usuario.Id,
            DispositivoNombre: dispositivo.Nombre,
            DispositivoId: dispositivo.Id));
    }


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
