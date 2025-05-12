using AppiNon.Models;
using AppiNon.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuraci�n de CORS
var corsPolicy = "PolicyCORS";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsPolicy,
        policy => policy.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
});

// 2. Configuraci�n base
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Configuraci�n de la base de datos (corregido "AppCon")
builder.Services.AddDbContext<PinonBdContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppCon")));

// 4. Configuraci�n de autenticaci�n JWT (corregido "secretkey" min�scula)
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

// 5. Configuraci�n de autorizaci�n por roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("1"));
    options.AddPolicy("User", policy => policy.RequireRole("2"));
});

// 6. Servicios en segundo plano
builder.Services.AddHostedService<StockPredictionService>();
builder.Services.AddHostedService<ReabastecimientoWorker>();

var app = builder.Build();

// Configuraci�n del pipeline HTTP

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
app.UseAuthentication();  // Primero autenticaci�n
app.UseAuthorization();   // Luego autorizaci�n

// D. Mapeo de controladores
app.MapControllers();

app.Run();