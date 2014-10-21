using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Touch.Domain;
using Touch.Logic;

namespace Touch.ServiceModel.Identity
{
    public class UserStore<TUser> :
        IUserClaimStore<TUser, Guid>,
        IUserEmailStore<TUser, Guid>,
        IUserLoginStore<TUser, Guid>,
        IUserRoleStore<TUser, Guid>,
        IUserPasswordStore<TUser, Guid>,
        IUserSecurityStampStore<TUser, Guid>
        where TUser : class, IIdentityUser
    {
        public UserStore()
        {
            return;
        }

        #region Dependencies
        public IdentityUserLogic<TUser> IdentityUserLogic { protected get; set; }
        public IdentityClaimLogic<TUser> IdentityClaimLogic { protected get; set; }
        public IdentityRoleLogic<TUser> IdentityRoleLogic { protected get; set; }
        #endregion

        #region User store methods

        #region IUserStore members
        public Task CreateAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            IdentityUserLogic.Insert(user);

            return Task.FromResult<object>(null);
        }

        public Task DeleteAsync(TUser user)
        {
            if (user != null)
                IdentityUserLogic.Delete(user);

            return Task.FromResult<Object>(null);
        }

        public Task<TUser> FindByIdAsync(Guid userId)
        {
            if (userId == Guid.Empty) throw new ArgumentException("Null or empty argument: userId");

            var result = IdentityUserLogic.GetUserById(userId);

            return Task.FromResult(result);
        }

        public Task<TUser> FindByNameAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName)) throw new ArgumentException("Null or empty argument: userName");

            var result = IdentityUserLogic.GetUserByName(userName);

            return Task.FromResult(result);
        }

        public Task UpdateAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            IdentityUserLogic.Update(user);

            return Task.FromResult<object>(null);
        }
        #endregion

        #region IUserClaimStore members
        public Task AddClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("user");

            IdentityClaimLogic.Insert(claim, user.Id);

            return Task.FromResult<object>(null);
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            var identity = IdentityClaimLogic.GetForUser(user);
            var roles = IdentityRoleLogic.GetForUser(user);

            identity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            return Task.FromResult<IList<Claim>>(identity.Claims.ToList());
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            IdentityClaimLogic.Delete(user, claim);

            return Task.FromResult<object>(null);
        }
        #endregion

        #region IUserEmailStore members
        public Task<TUser> FindByEmailAsync(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email");

            var result = IdentityUserLogic.GetUserByEmail(email);

            return Task.FromResult(result);
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            return Task.FromResult(true);
        }

        public Task SetEmailAsync(TUser user, string email)
        {
            user.Email = email;
            IdentityUserLogic.Update(user);

            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            return Task.FromResult(0);
        }
        #endregion

        #region IUserLoginStore members
        public Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            IdentityUserLogic.AddLogin(user, login);

            return Task.FromResult<object>(null);
        }

        public Task<TUser> FindAsync(UserLoginInfo login)
        {
            if (login == null) throw new ArgumentNullException("login");

            var userId = IdentityUserLogic.FindUserIdByLogin(login);

            if (userId == default(Guid)) return Task.FromResult<TUser>(null);

            var user = IdentityUserLogic.GetUserById(userId);

            return Task.FromResult(user);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            var logins = IdentityUserLogic.FindByUserId(user.Id);

            return Task.FromResult(logins);
        }

        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            IdentityUserLogic.Delete(user, login);

            return Task.FromResult<Object>(null);
        }
        #endregion

        #region IUserRoleStore members
        public Task AddToRoleAsync(TUser user, string roleName)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (string.IsNullOrEmpty(roleName)) throw new ArgumentException("Argument cannot be null or empty: roleName.");

            var roleId = IdentityRoleLogic.GetRoleId(roleName);

            if (!string.IsNullOrEmpty(roleId))
                IdentityRoleLogic.Insert(user, roleId);

            return Task.FromResult<object>(null);
        }

        public Task<IList<string>> GetRolesAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            var roles = IdentityRoleLogic.GetForUser(user);

            return Task.FromResult(roles);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (string.IsNullOrEmpty(roleName)) throw new ArgumentNullException("roleName");

            var roles = IdentityRoleLogic.GetForUser(user);

            return Task.FromResult(roles != null && roles.Contains(roleName));
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IUserPasswordStore members
        public Task<string> GetPasswordHashAsync(TUser user)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            user.PasswordHash = passwordHash;

            return Task.FromResult<Object>(null);
        }
        #endregion

        #region IUserSecurityStampStore members
        public Task<string> GetSecurityStampAsync(TUser user)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            user.SecurityStamp = stamp;

            return Task.FromResult(0);
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
        } 
        #endregion

        #endregion
    }
}
