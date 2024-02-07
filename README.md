# NodeHoster

A .NET solution aimed to simplify and provide the possibility to use IIS capabilities while hosting NodeJS applications on Windows Server.

Originally created by [Hallbergs](https://github.com/Hallbergs) and later modified by [jesade-vbg](https://github.com/jesade-vbg) to include AD group lookup and a user cache etc.

## Why?

I wanted to provide a way to host a NodeJS application on Windows server, without the hassle of dealing with setting up PM2 as a service etc. I also wanted to use the provided Windows Authentication from IIS, and pass the logged on user to the NodeJS application.

## .NET Version and Visual Studio

Supported by .Net 8.0 and Visual Studio 2022

## Application structure

The application contains three parts:

- A reverse proxy (YARP)
- A middleware adding name of the authenticated user to a header key of choice
- The middleware can also lookup AD groups for the user and add them to a header key of choice
- A service running a NodeJS application as a separate process

When the application is deployed to IIS, it will make sure that a proper version of Node is installed on the server, it will then start the NodeJS application and proxy all requests there.

The application listens to changes to the `.env` file in the `node_directory` (the directory set in `appSettings.json` that should contain the NodeJs application), and will restart the NodeJS application on changes to that file.

## Deploying the application

The solution is to be deployed to IIS on Windows server. Publish the solution, and review the settings in `appSettings.json`:

```json
{
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "noderoute": {
        "ClusterId": "nodecluster",
        "Match": {
          "Path": "{**catch-all}"
        }
      }
    },
    "Clusters": {
      "nodecluster": {
        "Destinations": {
          "node": {
            "Address": "http://localhost:3002" <--- Adress of the NodeJS application
          }
        }
      }
    }
  },
  "Serilog": {
    "Using": [],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Yarp": "Warning"
      }
    },
    "Enrich": [ "FromLogContext" ],
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs\\log.txt"
        }
      }
    ]
  },
  "ActiveDirectory": {
    "IsActive": true,
    "TrustedUserHeader": "X-Control-Header", <--- Key in which logged on user is sent
    "OverrideUserWithValue": "", <--- Override value for logged on user
    "TrustedGroupHeader": "X-Control-Group-Header", <--- Key in which logged on users groups is sent
    "OverrideGroupsWithValue": "" <--- Override value for users groups, comma separated.
  },
  "UserCache": {
    "TimeOut":  7200 <--- Seconds. The timeout for cached users and groups. Timeout is per user. (7200s = 2 hours)
  },
  "NodeHost": {
    "Enabled": "true", <--- Should the application host the NodeJS-application?
    "MinimumVersion": "20", <--- Minimum Node version required
    "EntryPoint": "index.js", <--- Which script should be ran to start the app?
    "FolderName": "node_server", <--- Folder containing the NodeJs-application
    "InstallNodeModules":  "false" <--- Should the app try to install node_modules?
  }
}
```

When you've made sure to check all the settings, you'll have to set up a new site in IIS and enable Windows Authentication. If the site fails to start, make sure to have a look at the log-directory in the project root.

## TODO

This solution was thrown together, and there are probably several bugs etc. One known bug is that the installation of `node_modules` will sometimes fail. This is probably due to insufficient folder permissions. **My suggestion would be to install the node_modules manually before deploying the solution to IIS.**

Potential additions:

- Instead of just listening for changes in `.env`, adding a setting for which file types should be monitored would be good.
- TLS Support (Currently using DirectorySearcher which probably not have support for it)
