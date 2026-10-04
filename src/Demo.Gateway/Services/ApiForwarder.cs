namespace Demo.Gateway.Services;

public sealed class ApiForwarder(IHttpClientFactory factory)
{
    public async Task ForwardAsync(HttpContext context)
    {
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path.Value);
            if (context.Request.Method == "POST")
            {
                request.Content = new StreamContent(context.Request.Body);
                request.Content.Headers.ContentType = new("application/json");
            }
            using var response = await factory.CreateClient("api").SendAsync(request, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            context.Response.Headers.CacheControl = "no-store";
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (HttpRequestException) { context.Response.StatusCode = 502; }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested) { context.Response.StatusCode = 504; }
    }
}
