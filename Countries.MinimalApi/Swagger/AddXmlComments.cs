using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Countries.MinimalApi.Swagger;

public static class SwaggerXmlComments
{
    public static void AddXmlComments(this SwaggerGenOptions options)
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        options.IncludeXmlComments(xmlPath);
    }
}