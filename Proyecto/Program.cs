using Microsoft.EntityFrameworkCore;
using Amazon;
using Amazon.S3;
using PryectoDeGraduacionBACK.Services.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Proyecto.Filters;
using Proyecto.Services.Email;
using Proyecto.Services.Recurrente;
using System.Text;
using Proyecto.Models;
using Proyecto.Services.Checkout;
using Proyecto.Services.Stock;

var builder = WebApplication.CreateBuilder(args);
var awsRegion = builder.Configuration["AWS:Region"];
var awsAccessKey = builder.Configuration["AWS:AccessKey"];
var awsSecretKey = builder.Configuration["AWS:SecretKey"];

const long maxUploadSize = 50 * 1024 * 1024;

builder.Services.AddScoped<CheckoutCalculatorService>();
builder.Services.AddScoped<StockReservationService>();
builder.Services.AddHostedService<StockReservationWorker>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadSize;
    options.MultipartHeadersLengthLimit = 32 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxUploadSize;
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = maxUploadSize;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa únicamente el JWT."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });

    options.DocumentFilter<PublicEndpointsDocumentFilter>();
});

//Recurrente
builder.Services.Configure<RecurrenteSettings>(
    builder.Configuration.GetSection("Recurrente"));

builder.Services.AddHttpClient("Recurrente", (serviceProvider, client) =>
{
    var settings = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<RecurrenteSettings>>()
        .Value;

    client.BaseAddress = new Uri(settings.BaseUrl);
    client.DefaultRequestHeaders.Add("X-SECRET-KEY", settings.SecretKey);
});

builder.Services.AddScoped<IRecurrenteService, RecurrenteService>();

//JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });
builder.Services.AddAuthorization();

//Amazon S3
builder.Services.AddSingleton<IAmazonS3>(_ =>
    new AmazonS3Client(
        awsAccessKey,
        awsSecretKey,
        RegionEndpoint.GetBySystemName(awsRegion)
    ));

builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IEmailService, MailtrapEmailService>();
builder.Services.AddScoped<IOrderEmailService, OrderEmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
        else
        {
            policy
                .WithOrigins(
                    "https://TU-FRONTEND-DOMINIO.com"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

string connectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("DefaultConnection")!
    : BuildConnectionStringFromUrl(
        Environment.GetEnvironmentVariable("DATABASE_URL")!
    );

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();

static string BuildConnectionStringFromUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={uri.Host};Port={uri.Port};Database={database};" +
           $"Username={userInfo[0]};Password={Uri.UnescapeDataString(userInfo[1])};" +
           $"SSL Mode=Prefer;Trust Server Certificate=true";
}