using TrustPanel.Api.Responses;
using TrustPanel.Application.Billing;

namespace TrustPanel.Api.Endpoints;

public static class StripeWebhookEndpoints
{
    public static void MapStripeWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/stripe", async (HttpContext httpContext, IBillingService billing) =>
        {
            string payload;
            using (var reader = new StreamReader(httpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            var signature = httpContext.Request.Headers["Stripe-Signature"].FirstOrDefault() ?? string.Empty;

            await billing.HandleWebhookAsync(payload, signature, httpContext.RequestAborted);
            return ApiResults.Ok(new { received = true }, "Webhook processed.");
        });
    }
}
