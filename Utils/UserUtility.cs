using NodeHoster.Middlewares;
using System.Text.RegularExpressions;
using System.DirectoryServices;
using Serilog;


namespace NodeHoster.Utils
{

    public class UserData
    {
        public UserData()
        {

            UserName = "";
            Groups = new List<string>();
            Email = "";
            Updated = DateTime.Now.AddYears(-1);
        }

        public string UserName { get; set; }
        public List<string> Groups { get; set; }
        public string Email{ get; set; }
        public DateTime Updated { get; set; }

    }

    public sealed class UserUtility
    {

        private static readonly Lazy<UserUtility> lazy = new Lazy<UserUtility>(() => new UserUtility());
        public Dictionary<string, UserData> Users { get; set; }
        private double CacheTimeOut { get; set; } = 3600;

        public static UserUtility Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private UserUtility()
        {
            Users = new Dictionary<string, UserData>();
            CacheTimeOut = Convert.ToDouble(ConfigurationUtility.GetSectionItem("UserCache:TimeOut"));
        }

        public UserData addUser(string userName)
        {

            userName = cleanUserName(userName);

            var user = new UserData();
            user.UserName = userName;
            user.Updated = DateTime.Now;

#pragma warning disable CA1416 // Validate platform compatibility
            string searchFilter = $"(&(objectCategory=User)(samAccountName={user.UserName}))";
            DirectorySearcher searcher = new();
            searcher.Filter = searchFilter;
            SearchResultCollection searchResults = searcher.FindAll();
#pragma warning restore CA1416 // Validate platform compatibility

            setGroups(user, searchResults);
            setEmail(user, searchResults);

            if (!Users.ContainsKey(userName))
            {
                Users.Add(userName, user);
            }
            else
            {
                Users[userName] = user;
            }

            return user;
        }

        public UserData? getUser(string userName)
        {

            userName = cleanUserName(userName);

            if (!Users.ContainsKey(userName))
            {
                return null;
            }

            var user = Users[userName];

            TimeSpan ts = DateTime.Now - user.Updated;

            if (ts.TotalSeconds < CacheTimeOut)
            {
                return user;
            }

            return null;
        }

        private string cleanUpGroup(string cnString)
        {
            string pattern = @"CN=([A-Za-z_0-9 -]*)";
            string input = cnString;
            RegexOptions options = RegexOptions.Multiline;

            string group = "";

            try
            {
                group = Regex.Matches(input, pattern, options)[0].ToString().Substring(3);
            }
            catch (Exception)
            {

                return "";
            }

            Log.Verbose("[UserUtility.cleanUpGroup] Input cnString '{0}' becomes group '{1}'", cnString, group);
            return group;
        }

        private string cleanUserName(string userName)
        {
            if (userName.IndexOf("\\") > -1)
            {
                return userName.Split('\\')[1];
            }
            return userName;
        }

        private void setGroups(UserData user, SearchResultCollection results)
        {
#pragma warning disable CA1416 // Validate platform compatibility

            if (results == null)
            {
                Log.Information("[UserUtility.setGroups] No groups found for user '{0}'", user.UserName);
                return;
            }

            foreach (SearchResult result in results)
            {
                foreach (String group in result.Properties["memberOf"])
                {
                    Log.Information("[UserUtility.setGroups] Adding group '{0}' for user '{1}'", group, user.UserName);
                    user.Groups.Add(cleanUpGroup(group));
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility

        }

        private void setEmail(UserData user, SearchResultCollection results)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            if (results == null)
            {
                Log.Information("[UserUtility.setEmail] No email found for user '{0}'", user.UserName);
                return;
            }

            string email = "";

            foreach (SearchResult result in results)
            {
                foreach (String _email in result.Properties["mail"])
                {
                    if(email.Length == 0)
                    {
                        // only use the first email found
                        email = _email;
                        Log.Information("[UserUtility.setEmail] Adding email '{0}' for user '{1}'", email, user.UserName);
                        user.Email = email;
                    }
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility

        }


    }
}
