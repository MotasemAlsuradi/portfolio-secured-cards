
using PortfolioSecuredCards.Api.Identity;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Infrastructure;
using PortfolioSecuredCards.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure services (DbContext, Repositories, and Unit of Work).
builder.Services.AddInfrastructure();

// Current Customer
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentCustomer, CurrentCustomer>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(PortfolioSecuredCards.Application.AssemblyReference).Assembly));

var app = builder.Build();

// Seed demo customers and portfolios.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await AppDbSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
