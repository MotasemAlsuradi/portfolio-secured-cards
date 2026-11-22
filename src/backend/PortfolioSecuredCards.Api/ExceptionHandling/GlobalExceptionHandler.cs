
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PortfolioSecuredCards.Domain.Constants;
using PortfolioSecuredCards.Domain.Exceptions;

namespace PortfolioSecuredCards.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = GetStatusCode(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception, 
                "Unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode}.",
                statusCode);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = statusCode >= 500 ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is DomainException domainException)
        {
            problem.Extensions["errorCode"] = domainException.ErrorCode;
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            DomainException domainException => GetDomainStatusCode(domainException.ErrorCode),
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static int GetDomainStatusCode(string errorCode)
    {
        return errorCode switch
        {
            BusinessErrorCodes.PortfolioFrozen
                or BusinessErrorCodes.InsufficientAvailableValue
                or BusinessErrorCodes.CreditLimitExceeded
                or BusinessErrorCodes.CardNotActive
                or BusinessErrorCodes.OutstandingBalance
                or BusinessErrorCodes.CardAlreadyClosed
                or BusinessErrorCodes.LienAlreadyReleased
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Internal Server Error"
        };
    }
}