using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Proyecto.Filters;

public class PublicEndpointsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var publicPaths = swaggerDoc.Paths
            .Where(path => path.Key.Contains("/public/", StringComparison.OrdinalIgnoreCase));

        foreach (var publicPath in publicPaths)
        {
            if (publicPath.Value is null)
            {
                continue;
            }

            var operations = publicPath.Value.Operations;
            if (operations is null)
            {
                continue;
            }

            foreach (var operation in operations.Values)
            {
                if (operation is null)
                {
                    continue;
                }

                operation.Security = new List<OpenApiSecurityRequirement>();
            }
        }
    }
}
