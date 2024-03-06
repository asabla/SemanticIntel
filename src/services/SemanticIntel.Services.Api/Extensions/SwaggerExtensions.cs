using Microsoft.OpenApi.Any;

using SemanticIntel.Core.Memory.Models;
using SemanticIntel.Services.Api.Filters;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace SemanticIntel.Services.Api.Extensions;

/// <summary>
/// Swagger extensions.
/// </summary>
/// <remarks>
/// This class is used to add Swagger to the WebApplicationBuilder and WebApplication.
/// </remarks>
internal static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger to the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder.</param>
    /// <returns>The WebApplicationBuilder.</returns>
    /// <remarks>
    /// This method is used to add Swagger to the WebApplicationBuilder.
    /// </remarks>
    public static WebApplicationBuilder AddSwagger(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                name: "v1",
                info: new()
                {
                    Title = "SemanticIntel.Services.Api",
                    Version = "v1"
                });

            options.AddFormFile();
            options.MapType<UploadTag>(() => new()
            {
                Type = "string",
                Default = new OpenApiString("tagName:tagValue")
            });
        });

        return builder;
    }

    /// <summary>
    /// Adds Swagger to the WebApplication.
    /// </summary>
    /// <param name="app">The WebApplication.</param>
    /// <returns>The WebApplication.</returns>
    /// <remarks>
    /// This method is used to add Swagger to the WebApplication.
    /// </remarks>
    public static WebApplication AddSwagger(
        this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseStatusCodePages();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "SemanticIntel.Services.Api v1");
        });

        return app;
    }

    public static void AddFormFile(this SwaggerGenOptions options)
        => options.OperationFilter<FormFileOperationFilter>();
}