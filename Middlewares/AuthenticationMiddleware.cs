using NodeHoster.Utils;
using System.Collections;




namespace NodeHoster.Middlewares
{

    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<AuthenticationMiddleware> _logger;


        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            this.next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string userHeaderKey = ConfigurationUtility.GetSectionItem("ActiveDirectory:TrustedUserHeader");
            string groupHeaderKey = ConfigurationUtility.GetSectionItem("ActiveDirectory:TrustedGroupHeader");
            var userOverride = ConfigurationUtility.GetSectionItem("ActiveDirectory:OverrideUserWithValue");
            var groupOverride = ConfigurationUtility.GetSectionItem("ActiveDirectory:OverrideGroupsWithValue");

            var userName = context.Request.HttpContext.User?.Identity?.Name ?? null;
            if (!string.IsNullOrEmpty(userOverride))
            {
                _logger.LogInformation("[AuthenticationMiddleware] BE AWARE - AD-user override is active! Override set to {@userOverride}.", userOverride);
                userName = userOverride;
            }

            if(string.IsNullOrEmpty(userName))
            {
                _logger.LogInformation("[AuthenticationMiddleware] userName was null or empty, continuing without setting headers.");
                await next(context);
                return;
            }

            var user = UserUtility.Instance.getUser(userName);

            if (user == null)
            {
                user = UserUtility.Instance.addUser(userName);
                if (user != null)
                {
                    _logger.LogInformation("Added user {@userName} with groups to cache", userName);
                }
                
            }

            string groups = String.Join(",", user.Groups.ToArray());


            if (!string.IsNullOrEmpty(groupOverride))
            {
                _logger.LogInformation("[AuthenticationMiddleware] BE AWARE - AD-groups override is active! Override set to {@groupOverride}.", groupOverride);
                groups = groupOverride;
            }

            context.Request.Headers.Add(userHeaderKey, userName);
            context.Request.Headers.Add(groupHeaderKey, groups);

            await next(context);
            return;

        }
    }
}
