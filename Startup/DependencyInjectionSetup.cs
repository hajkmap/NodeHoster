using Microsoft.AspNetCore.Authentication.Negotiate;
using NodeHoster.Utils;

namespace NodeHoster.Startup
{
    public static class DependencyInjectionSetup
    {

        public static async Task<IServiceCollection> RegisterServices(this IServiceCollection services)
        {
            bool AdIsActive = ConfigurationUtility.GetSectionItem("ActiveDirectory:IsActive").ToLower() == "true";
            NodeInstallationUtility NodeUtils = new NodeInstallationUtility();
            await NodeUtils.StartNodeServer();

            services.AddControllers();

            if (AdIsActive)
            {
                services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
                services.AddAuthorization(options => {
                    options.FallbackPolicy = options.DefaultPolicy;
                });
            }

            services.AddReverseProxy().LoadFromConfig(ConfigurationUtility.GetSection("ReverseProxy"));

            return services;
        }
    }
}
