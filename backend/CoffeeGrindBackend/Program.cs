using CoffeeGrindBackend.Extensions;
using CoffeeGrindBackend.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.AddApplicationServices();

var app = builder.Build();
app.ConfigureMiddleware();
app.MapGrindEndpoints();

app.Run();
