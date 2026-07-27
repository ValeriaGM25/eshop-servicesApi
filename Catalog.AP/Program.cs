
using Catalog.API.Exceptions;
using BuildingBlocks.Exceptions.Handler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    });

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);


builder.Services.AddCarter();

builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);

}).UseLightweightSessions();



var app = builder.Build();

app.MapCarter();

app.Run();
