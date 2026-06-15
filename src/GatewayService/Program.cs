var builder = WebApplication.CreateBuilder(args);

// Reverse proxy routes and clusters are defined in appsettings.json under "ReverseProxy".
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());
app.MapReverseProxy();

app.Run();
