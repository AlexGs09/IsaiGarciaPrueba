using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Application.Auth;
using Backend.Application.Usuarios;
using Backend.Contracts.Auth;
using Backend.Infrastructure.Auth;
using Backend.Infrastructure.Configuration;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Usuarios;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var solutionEnvPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env"));
if (File.Exists(solutionEnvPath))
{
    Env.Load(solutionEnvPath);
}

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<ConfiguracionAutenticacion>(options =>
{
    options.MaximoIntentosFallidosLogin = ObtenerEnteroEntorno("MAXIMO_INTENTOS_FALLIDOS_LOGIN", 4);
    options.MinutosBloqueoPorIntentos = ObtenerEnteroEntorno("MINUTOS_BLOQUEO_POR_INTENTOS", 15);
    options.MinutosDuracionTokenAcceso = ObtenerEnteroEntorno("MINUTOS_DURACION_TOKEN_ACCESO", 20);
    options.DiasDuracionRefreshToken = ObtenerEnteroEntorno("DIAS_DURACION_REFRESH_TOKEN", 7);
    options.EmisorJwt = Environment.GetEnvironmentVariable("EMISOR_JWT") ?? "Backend.PruebaTecnica";
    options.AudienciaJwt = Environment.GetEnvironmentVariable("AUDIENCIA_JWT") ?? "Frontend.PruebaTecnica";
    options.LlaveSecretaJwt = Environment.GetEnvironmentVariable("LLAVE_SECRETA_JWT")
        ?? "CLAVE_SUPER_SECRETA_DESARROLLO_2026_PRUEBA_TECNICA";
});
var configuracionJwt = new ConfiguracionAutenticacion
{
    MaximoIntentosFallidosLogin = ObtenerEnteroEntorno("MAXIMO_INTENTOS_FALLIDOS_LOGIN", 4),
    MinutosBloqueoPorIntentos = ObtenerEnteroEntorno("MINUTOS_BLOQUEO_POR_INTENTOS", 15),
    MinutosDuracionTokenAcceso = ObtenerEnteroEntorno("MINUTOS_DURACION_TOKEN_ACCESO", 20),
    DiasDuracionRefreshToken = ObtenerEnteroEntorno("DIAS_DURACION_REFRESH_TOKEN", 7),
    EmisorJwt = Environment.GetEnvironmentVariable("EMISOR_JWT") ?? "Backend.PruebaTecnica",
    AudienciaJwt = Environment.GetEnvironmentVariable("AUDIENCIA_JWT") ?? "Frontend.PruebaTecnica",
    LlaveSecretaJwt = Environment.GetEnvironmentVariable("LLAVE_SECRETA_JWT")
        ?? "CLAVE_SUPER_SECRETA_DESARROLLO_2026_PRUEBA_TECNICA"
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuracionJwt.EmisorJwt,
            ValidAudience = configuracionJwt.AudienciaJwt,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuracionJwt.LlaveSecretaJwt)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IFabricaConexionSql, FabricaConexionSql>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
builder.Services.AddScoped<IServicioUsuarios, ServicioUsuarios>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        message = "Backend .NET funcionando correctamente",
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/api/database", async () =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Problem(
            title: "Conexion no configurada",
            detail: "No se encontro la variable DATABASE_URL en el archivo .env.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT DB_NAME() AS DatabaseName, @@SERVERNAME AS ServerName", connection);
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Results.Problem(
                title: "Conexion incompleta",
                detail: "Se abrio la conexion pero no se pudo obtener informacion de la base de datos.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new
        {
            isConnected = true,
            database = reader["DatabaseName"]?.ToString(),
            server = reader["ServerName"]?.ToString(),
            auth = Environment.GetEnvironmentVariable("DB_AUTH") ?? "windows"
        });
    }
    catch (Exception exception)
    {
        return Results.Problem(
            title: "No fue posible conectarse a SQL Server",
            detail: exception.Message,
            statusCode: StatusCodes.Status500InternalServerError);
    }
});

app.MapPost("/api/auth/login", async (
    LoginRequestDto request,
    HttpContext httpContext,
    IServicioAutenticacion servicioAutenticacion,
    CancellationToken cancellationToken) =>
{
    var erroresValidacion = ValidarModelo(request);

    if (erroresValidacion.Count > 0)
    {
        return Results.ValidationProblem(erroresValidacion);
    }

    var resultado = await servicioAutenticacion.IniciarSesionAsync(
        request,
        httpContext.Connection.RemoteIpAddress?.ToString(),
        httpContext.Request.Headers.UserAgent.ToString(),
        cancellationToken);

    if (resultado.Exitoso && resultado.Respuesta is not null)
    {
        return Results.Ok(resultado.Respuesta);
    }

    return Results.Json(
        resultado.Error,
        statusCode: resultado.CodigoEstadoHttp);
});

app.MapGet("/api/usuarios/perfil", async (
    HttpContext httpContext,
    IServicioUsuarios servicioUsuarios,
    CancellationToken cancellationToken) =>
{
    var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? httpContext.User.FindFirst("sub")?.Value;

    if (!Guid.TryParse(sub, out var usuarioId))
    {
        return Results.Json(
            new { codigo = "token_invalido", mensaje = "No se pudo identificar al usuario autenticado." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var perfil = await servicioUsuarios.ObtenerPerfilAsync(usuarioId, cancellationToken);
    if (perfil is null)
    {
        return Results.Json(
            new { codigo = "usuario_no_encontrado", mensaje = "No se encontro el perfil del usuario autenticado." },
            statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(perfil);
})
.RequireAuthorization();

app.MapPost("/api/auth/refresh", async (
    RefreshTokenRequestDto request,
    HttpContext httpContext,
    IServicioAutenticacion servicioAutenticacion,
    CancellationToken cancellationToken) =>
{
    var erroresValidacion = ValidarModelo(request);
    if (erroresValidacion.Count > 0)
    {
        return Results.ValidationProblem(erroresValidacion);
    }

    var resultado = await servicioAutenticacion.RenovarTokenAsync(
        request.RefreshToken,
        httpContext.Connection.RemoteIpAddress?.ToString(),
        httpContext.Request.Headers.UserAgent.ToString(),
        cancellationToken);

    if (resultado.Exitoso && resultado.Respuesta is not null)
    {
        return Results.Ok(resultado.Respuesta);
    }

    return Results.Json(
        new { codigo = resultado.Codigo, mensaje = resultado.Mensaje },
        statusCode: resultado.CodigoEstado);
});

app.MapPost("/api/auth/logout", async (
    HttpContext httpContext,
    IServicioAutenticacion servicioAutenticacion,
    CancellationToken cancellationToken) =>
{
    var sub = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
        ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? httpContext.User.FindFirst("sub")?.Value;

    var tokenJti = httpContext.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
        ?? httpContext.User.FindFirst("jti")?.Value;

    if (!Guid.TryParse(sub, out var usuarioId) || string.IsNullOrWhiteSpace(tokenJti))
    {
        return Results.Json(
            new { codigo = "token_invalido", mensaje = "No se pudo identificar la sesion autenticada." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var resultado = await servicioAutenticacion.CerrarSesionAsync(usuarioId, tokenJti, cancellationToken);

    return Results.Json(
        new { codigo = resultado.Codigo, mensaje = resultado.Mensaje },
        statusCode: resultado.CodigoEstado);
})
.RequireAuthorization();

app.Run();

static int ObtenerEnteroEntorno(string nombreVariable, int valorPorDefecto)
{
    var valor = Environment.GetEnvironmentVariable(nombreVariable);
    return int.TryParse(valor, out var entero) ? entero : valorPorDefecto;
}

static Dictionary<string, string[]> ValidarModelo<T>(T modelo)
{
    var validationContext = new ValidationContext(modelo!);
    var validationResults = new List<ValidationResult>();
    Validator.TryValidateObject(modelo!, validationContext, validationResults, true);

    return validationResults
        .SelectMany(resultado => resultado.MemberNames.DefaultIfEmpty(string.Empty),
            (resultado, memberName) => new { memberName, resultado.ErrorMessage })
        .GroupBy(item => string.IsNullOrWhiteSpace(item.memberName) ? "general" : item.memberName)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(item => item.ErrorMessage ?? "Error de validacion.")
                .ToArray());
}
