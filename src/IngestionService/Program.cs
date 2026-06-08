using IngestionService.Endpoints;
using IngestionService.Services;
using Microsoft.EntityFrameworkCore;
using SensorMonitoring.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IReadingIngestionService, ReadingIngestionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok());
app.MapReadingEndpoints();

app.Run();
