using System;
using System.Data.Common;
using System.Web.Configuration;

namespace Touch.Configuration
{
    /// <summary>
    /// Environment values.
    /// </summary>
    public static class EnvironmentValues
    {
        #region .ctor
        static EnvironmentValues()
        {
            var appSettings = WebConfigurationManager.AppSettings;
            var connectionStrings = WebConfigurationManager.ConnectionStrings;

            #region Environment
            //Beanstalk
            if (!string.IsNullOrEmpty(appSettings["PARAM1"]))
            {
                CurrentEnvironment = appSettings["PARAM1"];
            }

            //System environment
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOUCH_ENVIRONMENT")))
            {
                CurrentEnvironment = Environment.GetEnvironmentVariable("TOUCH_ENVIRONMENT");
            }
            #endregion

            #region Touch database
            //Beanstalk
            if (!string.IsNullOrEmpty(appSettings["PARAM2"]))
            {
                TouchDatabase = appSettings["PARAM2"];
            }

            //System environment
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOUCH_DATABASE")))
            {
                TouchDatabase = Environment.GetEnvironmentVariable("TOUCH_DATABASE");
            }

            //Configuration file
            if (connectionStrings["TouchDatabase"] != null)
            {
                TouchDatabase = connectionStrings["TouchDatabase"].ConnectionString;
            }
            #endregion

            #region AWS
            //Beanstalk
            if (!string.IsNullOrEmpty(appSettings["AWSAccessKey"]) && !string.IsNullOrEmpty(appSettings["AWSAccessKey"]))
            {
                AwsKey = appSettings["AWSAccessKey"];
                AwsSecret = appSettings["AWSSecretKey"];
            }

            //System environment
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_KEY")) &&
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET")))
            {
                AwsKey = Environment.GetEnvironmentVariable("AWS_KEY");
                AwsSecret = Environment.GetEnvironmentVariable("AWS_SECRET");
            }

            //Configuration file
            if (connectionStrings["AWS"] != null)
            {
                var builder = new DbConnectionStringBuilder();
                builder.ConnectionString = connectionStrings["AWS"].ConnectionString;

                if (builder.ContainsKey("AWSAccessKeyId") && builder.ContainsKey("SecretAccessKey"))
                {
                    AwsKey = builder["AWSAccessKeyId"].ToString();
                    AwsSecret = builder["SecretAccessKey"].ToString();
                }
            }
            #endregion

            #region Authentication
            var authenticationString = GetConnectionString("Authentication");

            if (!string.IsNullOrEmpty(authenticationString))
            {
                var builder = new DbConnectionStringBuilder { ConnectionString = authenticationString };
                AuthenticationKey = builder["AuthenticationKey"] as string;
                AuthenticationSecret = builder["AuthenticationSecret"] as string;
            }

            //Beanstalk
            if (!string.IsNullOrEmpty(appSettings["PARAM4"]) && !string.IsNullOrEmpty(appSettings["PARAM5"]))
            {
                AuthenticationKey = appSettings["PARAM4"];
                AuthenticationSecret = appSettings["PARAM5"];
            }
            #endregion
        }
        #endregion

        #region Properties
        /// <summary>
        /// Current environment name.
        /// </summary>
        public static string CurrentEnvironment { get; private set; }

        /// <summary>
        /// AWS key.
        /// </summary>
        public static string AwsKey { get; private set; }

        /// <summary>
        /// AWS secret.
        /// </summary>
        public static string AwsSecret { get; private set; }

        /// <summary>
        /// Touch database connection string.
        /// </summary>
        public static string TouchDatabase { get; private set; }

        /// <summary>
        /// Authentication key.
        /// </summary>
        public static string AuthenticationKey { get; private set; }

        /// <summary>
        /// Authentication secret.
        /// </summary>
        public static string AuthenticationSecret { get; private set; }
        #endregion

        #region Methods
        /// <summary>
        /// Get connection string for the current environment.
        /// </summary>
        /// <param name="name">Connection string name without an environment suffix.</param>
        /// <returns>Connection string or <c>null</c>.</returns>
        public static string GetConnectionString(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            var connectionStrings = WebConfigurationManager.ConnectionStrings;

            if (CurrentEnvironment != null)
            {
                var fullName = name + "." + CurrentEnvironment;

                if (connectionStrings[fullName] != null)
                    return connectionStrings[fullName].ConnectionString;
            }

            return connectionStrings[name] != null
                ? connectionStrings[name].ConnectionString
                : null;
        }
        #endregion
    }
}
