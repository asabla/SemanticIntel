using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace SemanticIntel.Services.Api.Filters;

internal class FormFileOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ParameterDescriptions is null)
            return;

        var paramDescriptors = context.ApiDescription.ParameterDescriptions
                .Where(e => e.Type == typeof(IFormFile) || e.Type == typeof(IFormFileCollection));

        foreach (var param in paramDescriptors)
        {
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    [param.Name] = param.Type == typeof(IFormFile)
                        ? new() { Type = "string", Format = "binary" }
                        : new() { Type = "array", Items = new() { Type = "string", Format = "binary" } }
                }
            };

            if (param.IsRequired)
                schema.Required.Add(param.Name);

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema,
                        Encoding = new Dictionary<string, OpenApiEncoding>
                        {
                            [param.Name] = new OpenApiEncoding { Style = ParameterStyle.Form }
                        }
                    }
                }
            };
        }
    }
}