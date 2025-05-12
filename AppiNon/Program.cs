using AppiNon.Models;
using AppiNon.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de CORS
var corsPolicy = "PolicyCORS";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy => policy.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
});

// 2. Configuración base
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Configuración de la base de datos (corregido "AppCon")
builder.Services.AddDbContext<PinonBdContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppCon")));

// 4. Configuración de autenticación JWT (corregido "secretkey" minúscula)
var jwtSettings = builder.Configuration.GetSection("settings");
var secretKey = jwtSettings["secretkey"] ?? throw new InvalidOperationException("Secret key no configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// 5. Configuración de autorización por roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("1"));
    options.AddPolicy("User", policy => policy.RequireRole("2"));
});

// 6. Servicios en segundo plano
builder.Services.AddHostedService<StockPredictionService>();
builder.Services.AddHostedService<ReabastecimientoWorker>();

var app = builder.Build();

// Configuración del pipeline HTTP

// A. CORS
app.UseCors(corsPolicy);

// B. Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// C. Middlewares esenciales (ORDEN IMPORTANTE)
app.UseHttpsRedirection();
app.UseAuthentication();  // Primero autenticación
app.UseAuthorization();   // Luego autorización

// D. Mapeo de controladores
app.MapControllers();

app.Run();