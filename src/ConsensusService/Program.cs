using ConsensusService;
using ConsensusService.Consensus;
using Microsoft.EntityFrameworkCore;
using SensorMonitoring.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<SensorDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<ConsensusOptions>(
    builder.Configuration.GetSection(ConsensusOptions.SectionName));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHostedService<ConsensusWorker>();

var host = builder.Build();
host.Run();
