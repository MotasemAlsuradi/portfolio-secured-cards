
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.OpenApi.Models;
using PortfolioSecuredCards.Api.BackgroundJobs;
using PortfolioSecuredCards.Api.ExceptionHandling;
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

// Global Exception Handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("CustomerId", new OpenApiSecurityScheme
    {
        Name = "X-Demo-Customer-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Customer Id (get it from GET /api/customers)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "CustomerId" }
            },
            Array.Empty<string>()
        }
    });
});


// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(PortfolioSecuredCards.Application.AssemblyReference).Assembly));

const string CorsPolicy = "AngularClient";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

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

app.UseExceptionHandler();

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

app.UseRouting();

app.UseCors(CorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose Program for integration testing.
public partial class Program { }