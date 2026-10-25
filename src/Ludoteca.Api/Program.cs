using System.Reflection;
using System.Security.Claims;
using System.Text;
using Ludoteca.Api.Http;
using Ludoteca.Application;
using Ludoteca.Infrastructure;
using Ludoteca.Infrastructure.Persistence;
using Ludoteca.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRouting(opcoes => opcoes.LowercaseUrls = true);
builder.Services.AddResponseCompression(opcoes => opcoes.EnableForHttps = true);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>()
    ?? throw new InvalidOperationException("A seção de configuração 'Jwt' não foi encontrada.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ludoteca API",
        Version = "v1",
        Description = "API REST para gerenciar o acervo de jogos de tabuleiro, os membros e os empréstimos de uma ludoteca."
    });

    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe apenas o token JWT obtido em /api/auth/login.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var arquivoXml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(arquivoXml))
        opcoes.IncludeXmlComments(arquivoXml);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseResponseCompression();

app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Ludoteca API v1");
    opcoes.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.Services.InicializarBancoAsync();

app.Run();

public partial class Program
{
}
