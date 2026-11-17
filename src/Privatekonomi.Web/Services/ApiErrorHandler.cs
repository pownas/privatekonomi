using System.Text.Json;

using Privatekonomi.Web.Models;

namespace Privatekonomi.Web.Services;

/// <summary>
/// Service for handling API errors and ProblemDetails responses.
/// </summary>
public class ApiErrorHandler
{
    /// <summary>
    /// Extracts a user-friendly error message from an HTTP response.
    /// </summary>
    public static async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            // Try to parse as ProblemDetails
            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

            if (problemDetails != null)
            {
                // Build a user-friendly error message
                var message = problemDetails.Title ?? "Ett fel uppstod";
                
                if (!string.IsNullOrEmpty(problemDetails.Detail))
                {
                    message = problemDetails.Detail;
                }

                // Add validation errors if present
                if (problemDetails.Errors != null && problemDetails.Errors.Any())
                {
                    var validationMessages = problemDetails.Errors
                        .SelectMany(e => e.Value.Select(v => $"{e.Key}: {v}"))
                        .ToList();
                    
                    if (validationMessages.Any())
                    {
                        message += "\n" + string.Join("\n", validationMessages);
                    }
                }

                return message;
            }
        }
        catch
        {
            // If we can't parse as ProblemDetails, fall through to default handling
        }

        // Fallback to status code message
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => "Ogiltig förfrågan",
            System.Net.HttpStatusCode.Unauthorized => "Obehörig åtkomst",
            System.Net.HttpStatusCode.Forbidden => "Åtkomst nekad",
            System.Net.HttpStatusCode.NotFound => "Resursen hittades inte",
            System.Net.HttpStatusCode.Conflict => "Konflikt uppstod",
            System.Net.HttpStatusCode.InternalServerError => "Serverfel uppstod",
            System.Net.HttpStatusCode.ServiceUnavailable => "Tjänsten är inte tillgänglig",
            _ => $"Ett fel uppstod (HTTP {(int)response.StatusCode})"
        };
    }

    /// <summary>
    /// Checks if response is successful, throws exception with error message if not.
    /// </summary>
    public static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await GetErrorMessageAsync(response);
            throw new HttpRequestException(errorMessage);
        }
    }
}
