
using Hangfire;
using Hangfire.Dashboard;
using PortfolioSecuredCards.Api.BackgroundJobs;
using PortfolioSecuredCards.Api.Identity;
using PortfolioSecuredCards.Application.Abstractions.Identity;
using PortfolioSecuredCards.Application.Configuration;
using PortfolioSecuredCards.Infrastructure;
using PortfolioSecuredCards.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Register Infrastructure services (DbContext, Repositories, and Unit of Work).
builder.Services.AddInfrastructure();

// Current Customer
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentCustomer, CurrentCustomer>();

// Configure delinquency threshold.
var delinquencyOptions = builder.Configuration
    .GetSection("Delinquency")
    .Get<DelinquencyOptions>()
    ?? throw new InvalidOperationException("Delinquency configuration is missing.");

if (delinquencyOptions.ThresholdMinutes <= 0)
{
    throw new InvalidOperationException("Delinquency threshold must be greater than zero.");
}

builder.Services.AddSingleton(delinquencyOptions);

// Hangfire
builder.Services.AddHangfire(configuration => configuration.UseInMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddTransient<DelinquencyScanJob>();

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

// Schedule delinquency scan every hour.
var recurringJobManager = app.Services
    .GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<DelinquencyScanJob>(
    "credit-card-delinquency-scan",
    job => job.ExecuteAsync(),
    Cron.Hourly());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[]
        {
            new LocalRequestsOnlyAuthorizationFilter()
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();