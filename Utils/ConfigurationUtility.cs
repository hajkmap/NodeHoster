namespace NodeHoster.Utils
{
    public class ConfigurationUtility
    {
        public static IConfiguration GetConfiguration()
        {
            object? configurationObject = AppDomain.CurrentDomain.GetData("Configuration");
            if (configurationObject is not null && configurationObject is IConfiguration)
            {
                return configurationObject as IConfiguration;
            }
            else
            {
                throw new Exception("Configuration Error");
            }
        }

        public static IConfiguration GetSection(string sectionKeyPath)
        {
            IConfiguration configuration = GetConfiguration();
            return GetSection(configuration, sectionKeyPath);
        }

        public static IConfiguration GetSection(IConfiguration configuration, string sectionKeyPath)
        {
            return configuration.GetSection(sectionKeyPath);
        }

        public static string GetSectionItem(string sectionKeyPath)
        {
            IConfiguration configuration = GetConfiguration();
            return GetSectionItem(configuration, sectionKeyPath);
        }

        public static string GetSectionItem(IConfiguration configuration, string sectionKeyPath)
        {
            return configuration.GetSection(sectionKeyPath).Get<string>() ?? "";
        }
    }

}
