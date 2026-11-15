using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniSwap.Application.Common;
using UniSwap.Domain.Entities;

namespace UniSwap.WebApi;

public sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, code) = exception switch
        {
            ResourceNotFoundException => (404, "not_found"),
            UnauthorizedAccessException => (403, "forbidden"),
            RequestConflictException conflict => (409, conflict.Code),
            InvalidTransitionException => (409, ConflictCodes.StateConflict),
            DbUpdateConcurrencyException => (409, ConflictCodes.ConcurrentUpdate),
            DbUpdateException { InnerException: PostgresException
                { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.SerializationFailure } } =>
                (409, ConflictCodes.ConcurrentUpdate),
            InvalidImageUploadException => (400, "invalid_image"),
            ArgumentException => (400, "invalid_request"),
            _ => (500, "internal_error")
        };
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Status = status, Extensions = { ["code"] = code } }
        });
    }

    public static void AddDefaultCode(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var code = problem switch
        {
            ValidationProblemDetails => "validation_failed",
            { Status: 400 } => "invalid_request",
            { Status: 401 } => "unauthorized",
            { Status: 403 } => "forbidden",
            { Status: 404 } => "not_found",
            { Status: 413 } => "payload_too_large",
            { Status: 415 } => "unsupported_media_type",
            { Status: 500 } => "internal_error",
            _ => "http_error"
        };
        problem.Extensions.TryAdd("code", code);
    }
}
