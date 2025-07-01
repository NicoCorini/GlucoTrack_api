using Microsoft.EntityFrameworkCore;
using GlucoTrack_api.Models;
using GlucoTrack_api.Data;

var builder = WebApplication.CreateBuilder(args);

// Log della stringa di connessione effettiva
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("[FATAL] Connection string 'DefaultConnection' non trovata in appsettings.json. L'applicazione verrà terminata.");
    Environment.Exit(1);
}
Console.WriteLine($"[DEBUG] Connection string usata: {connectionString}");

// Add services to the container.
builder.Services.AddDbContext<GlucoTrackDBContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
