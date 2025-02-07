using System.Diagnostics;

namespace AppHost;

internal static class ResourceBuilderExtensions
{
    internal static IResourceBuilder<T> WithSwaggerUi<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints // apply this extension method only to resources that have endpoints
    {
        // Add Swagger UI to the resource
       return builder.WithOpenApiDocs("swagger", "Swagger UI", "swagger");
    }
    
    internal static IResourceBuilder<T> WithScalarUi<T>(this IResourceBuilder<T> builder)
        where T : IResourceWithEndpoints
    {
        // Add Swagger UI to the resource
        return builder.WithOpenApiDocs("scalar", "Scalar UI", "scalar/v1");
    }
    private static IResourceBuilder<T> WithOpenApiDocs<T>(this IResourceBuilder<T> builder,string name, 
        string displayName,
        string openApiPath)
    where T : IResourceWithEndpoints
    {
        // Add OpenAPI documentation to the resource
        return builder.WithCommand(name, displayName,
            executeCommand: _ =>
            {
                try
                {
                    var endpoint = builder.GetEndpoint("https");
                    var url = $"{endpoint.Url}/{openApiPath}";
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return Task.FromResult(new ExecuteCommandResult { Success = true });
                }
                catch (Exception e)
                {
                    return Task.FromResult(new ExecuteCommandResult { Success = false, ErrorMessage = e.Message });
                }

            });
    }
}