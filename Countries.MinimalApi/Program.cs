using Azure.Identity;
using Countries.MinimalApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var keyVaultUri = builder.Configuration.GetValue<string>("KeyVault:Uri");
if (!string.IsNullOrEmpty(keyVaultUri))
    try
    {
        builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not connect to Azure Key Vault: {ex.Message}");
    }

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddAuthenticationServices();
builder.Services.AddApiVersioningServices();
builder.Services.AddSwaggerServices();
builder.Services.AddRateLimitingServices();
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddExternalServices();
builder.Services.AddExceptionHandlingServices();
builder.Services.AddCorsServices();
builder.Services.AddHostConfiguration();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Countries API V1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "Countries API V2");

    options.DocExpansion(DocExpansion.List);
    options.DisplayRequestDuration();
    options.DefaultModelsExpandDepth(1);
});

app.UseCors("Restricted");
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler(opt => { });
app.UseRateLimiter();

app.MapHealthCheckEndpoints();
app.MapAuthenticationEndpoints();
app.MapCountryEndpoints();
app.MapCachedCountryEndpoints();
app.MapFileEndpoints();
app.MapStreamingEndpoints();
app.MapUtilityEndpoints();
app.MapRateLimitingEndpoints();
app.MapExceptionEndpoints();
app.MapRoutingExamples();
app.MapParameterBindingExamples();

app.Run();

public partial class Program
{
}