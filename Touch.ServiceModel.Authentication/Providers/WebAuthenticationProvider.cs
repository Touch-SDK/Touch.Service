using System;
using System.Web;
using System.Web.Security;
using Touch.Domain;
using Touch.Serialization;

namespace Touch.Providers
{
    /// <summary>
    /// Web authentication provider.
    /// </summary>
    sealed public class WebAuthenticationProvider : IAuthenticationProvider
    {
        #region Dependencies
        public ISerializer Serializer { private get; set; }
        #endregion

        #region IAuthenticationProvider implementation
        public Credentials ActiveConsumer
        {
            get
            {
                var context = HttpContext.Current;
                var identity = context.User != null
                    ? context.User.Identity as FormsIdentity
                    : null;

                if (identity == null) return null;

                if (identity.Ticket == null || string.IsNullOrEmpty(identity.Ticket.UserData))
                {
                    Logout();
                    return null;
                }

                Credentials credentials;

                if (context.Items.Contains("Credentials"))
                    credentials = (Credentials)context.Items["Credentials"];
                else
                    context.Items["Credentials"] = credentials = Serializer.Deserialize<Credentials>(identity.Ticket.UserData);

                return credentials;
            }
        }

        public void Authenticate(string username, Credentials credentials)
        {
            var context = HttpContext.Current;
            if (context == null || context.User == null) return;

            var data = Serializer.Serialize(credentials);

            var ticket = new FormsAuthenticationTicket(
                0,
                username,
                DateTime.Now,
                DateTime.Now.AddMinutes(2000),
                true,
                data
            );

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);

            context.Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket));
        }

        public void Logout()
        {
            var context = HttpContext.Current;
            var identity = context.User != null
                    ? context.User.Identity as FormsIdentity
                    : null;

            if (identity != null)
                FormsAuthentication.SignOut();

            context.Items.Remove("Credentials");
        }
        #endregion
    }
}
