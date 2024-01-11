using Serilog;

namespace NodeHoster.Startup
{
    public static class LoggingSetup
    {
        public static IHostBuilder ConfigureLogging(this IHostBuilder host)
        {
            IConfiguration configuration = AppDomain.CurrentDomain.GetData("Configuration") as IConfiguration;
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            host.UseSerilog();
            return host;
        }


    }
}
