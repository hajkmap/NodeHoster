﻿using NodeHoster.Middlewares;
using System.Text.RegularExpressions;
using System.DirectoryServices;


namespace NodeHoster.Utils
{

    public class UserData
    {
        public UserData()
        {

            UserName = "";
            Groups = new List<string>();
            Updated = DateTime.Now.AddYears(-1);
        }

        public string UserName { get; set; }
        public List<string> Groups { get; set; }
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
            setGroups(user);

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
            string pattern = @"CN=([A-Za-z_0-9 ]*)";
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

        private void setGroups(UserData user)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            string searchFilter = $"(&(objectCategory=User)(samAccountName={user.UserName}))";


            DirectorySearcher searcher = new();
            searcher.Filter = searchFilter;
            SearchResultCollection results = searcher.FindAll();

            foreach (SearchResult result in results)
            {
                foreach (String group in result.Properties["memberOf"])
                {
                    user.Groups.Add(cleanUpGroup(group));
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }




    }
}
