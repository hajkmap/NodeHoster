namespace NodeHoster.Startup
{
    public static class ConfigurationSetup
    {
        public static AppDomain SetupConfiguration(this AppDomain appDomain)
        {
            IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            appDomain.SetData("Configuration", configuration);
            return appDomain;
        }

    }

}
