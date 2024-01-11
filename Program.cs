using NodeHoster.Middlewares;
using NodeHoster.Startup;
using NodeHoster.Utils;

var builder = WebApplication.CreateBuilder(args);

AppDomain.CurrentDomain.SetupConfiguration();

builder.Host.ConfigureLogging();

await builder.Services.RegisterServices();

var app = builder.Build();



bool AdIsActive =  ConfigurationUtility.GetSectionItem("ActiveDirectory:IsActive").ToLower() == "true";

if (AdIsActive)
{
    app.UseAuthorization();
    app.UseMiddleware<AuthenticationMiddleware>();
}

app.MapReverseProxy();

app.Run();
