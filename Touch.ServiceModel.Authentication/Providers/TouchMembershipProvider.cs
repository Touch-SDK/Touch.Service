using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Security;
using Touch.Domain;
using Touch.Logic;
using Touch.Persistence;
using Membership = Touch.Domain.Membership;

namespace Touch.Providers
{
    /// <summary>
    /// Membership provider base.
    /// </summary>
    public class TouchMembershipProvider : MembershipProvider
    {
        #region .ctor
        public TouchMembershipProvider()
        {
        }

        public TouchMembershipProvider(string name, NameValueCollection config)
        {
            name = SetDefaultName(name);
            _providerName = name;
            config = SetConfigDefaults(config);

            ValidatingPassword += MembershipProviderValidatingPassword;
            SetConfigurationProperties(config);
        }
        #endregion

        #region Dependencies
        public MembershipLogic MembershipLogic { protected get; set; }
        #endregion

        #region Class Variables
        private readonly string _providerName;
        private string _applicationName;
        private bool _enablePasswordReset;
        private bool _enablePasswordRetrieval;
        private bool _requiresQuestionAndAnswer;
        private bool _requiresUniqueEmail;
        private int _maxInvalidPasswordAttempts;
        private int _passwordAttemptWindow;
        private MembershipPasswordFormat _passwordFormat;
        private int _minRequiredNonAlphanumericCharacters;
        private int _minRequiredPasswordLength;
        private string _passwordStrengthRegularExpression;
        private string _passwordHashAlgorithm;
        private string ProviderName { get { return _providerName ?? Name; } }
        #endregion

        #region Enums
        private enum FailureType
        {
            Password = 1
        }
        #endregion

        #region Properties
        public override string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; }
        }

        public override bool EnablePasswordReset
        {
            get { return _enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return _enablePasswordRetrieval; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return _requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return _requiresUniqueEmail; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return _maxInvalidPasswordAttempts; }
        }

        public override int PasswordAttemptWindow
        {
            get { return _passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return _passwordFormat; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return _minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return _minRequiredPasswordLength; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return _passwordStrengthRegularExpression; }
        }

        public string PasswordHashAlgorithm { get { return _passwordHashAlgorithm; } }
        #endregion

        #region Initialization
        public override void Initialize(string name, NameValueCollection config)
        {
            name = SetDefaultName(name);
            config = SetConfigDefaults(config);

            base.Initialize(name, config);

            ValidatingPassword += MembershipProviderValidatingPassword;
            SetConfigurationProperties(config);
        }

        private static string SetDefaultName(string name)
        {
            return String.IsNullOrEmpty(name)
                       ? "customProvider"
                       : name;
        }

        private static NameValueCollection SetConfigDefaults(NameValueCollection config)
        {
            if (config == null) throw new ArgumentNullException("config");

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "NHibernate Membership Provider");
            }

            if (string.IsNullOrEmpty(config["passwordHashAlgorithm"]))
            {
                config.Remove("passwordHashAlgorithm");
                config.Add("passwordHashAlgorithm", "SHA512");
            }

            return config;
        }

        private void SetConfigurationProperties(NameValueCollection config)
        {
            _applicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            _maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            _passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            _minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "0"));
            _minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "6"));
            _passwordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
            _enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            _enablePasswordRetrieval = Convert.ToBoolean(GetConfigValue(config["enablePasswordRetrieval"], "false"));
            _requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
            _requiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            _passwordHashAlgorithm = GetConfigValue(config["passwordHashAlgorithm"], "SHA512");
            SetPasswordFormat(config["passwordFormat"]);
        }

        private void SetPasswordFormat(string passwordFormat)
        {
            if (passwordFormat == null)
            {
                passwordFormat = "Clear";
            }

            switch (passwordFormat)
            {
                case "Hashed":
                    _passwordFormat = MembershipPasswordFormat.Hashed;
                    break;
                case "Encrypted":
                    _passwordFormat = MembershipPasswordFormat.Encrypted;
                    break;
                case "Clear":
                    _passwordFormat = MembershipPasswordFormat.Clear;
                    break;
                default:
                    throw new ProviderException("Password format not supported.");
            }
        }

        private static string GetConfigValue(string configValue, string defaultValue)
        {
            if (String.IsNullOrEmpty(configValue))
            {
                return defaultValue;
            }
            return configValue;
        }
        #endregion

        #region Query methods
        private Membership GetUserByToken(string token)
        {
            return MembershipLogic.GetUserByToken(token);
        }

        private Membership GetUserByName(string username)
        {
            return MembershipLogic.GetUserByName(username);
        }

        private string GetUserNameByEMail(string email)
        {
            return MembershipLogic.GetUserNameByEMail(email);
        }

        private IEnumerable<Membership> GetAllUsers(int pageIndex, int pageSize)
        {
            return MembershipLogic.GetAllUsers(pageIndex, pageSize);
        }

        private int GetNumberOfUsersOnline(DateTime compareTime)
        {
            return MembershipLogic.CountOnlineUsers(compareTime);
        }

        private void UpdateUser(Membership membership)
        {
            MembershipLogic.UpdateUser(membership);
        }

        private void SaveUser(Membership membership)
        {
            MembershipLogic.SaveUser(membership);
        }

        private void DeleteUser(Membership membership)
        {
            MembershipLogic.DeleteUser(membership);
        }
        #endregion

        #region Implemented Abstract Methods from MembershipProvider
        /// <summary>
        /// Change the user password.
        /// </summary>
        /// <param name="username">UserName</param>
        /// <param name="oldPwd">Old password.</param>
        /// <param name="newPwd">New password.</param>
        /// <returns>T/F if password was changed.</returns>
        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");

            if (!ValidateUser(username, oldPwd))
                return false;

            var args = new ValidatePasswordEventArgs(username, newPwd, false)
            {
                FailureInformation = new MembershipPasswordException("Change password canceled due to new password validation failure.")
            };

            OnValidatingPassword(args);

            if (args.Cancel)
                throw args.FailureInformation;

            try
            {
                var u = GetUserByName(username);
                var salt = GenerateSalt();

                u.Password = EncodePassword(newPwd, salt);
                u.Salt = salt;

                MembershipLogic.UpdateUser(u);
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Change the question and answer for a password validation.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="newPwdQuestion">New question text.</param>
        /// <param name="newPwdAnswer">New answer text.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPwdQuestion, string newPwdAnswer)
        {
            throw new ProviderException("Password questions are not available.");
        }

        /// <summary>
        /// Create a new user.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">Password.</param>
        /// <param name="email">Email address.</param>
        /// <param name="passwordQuestion">Security quesiton for password.</param>
        /// <param name="passwordAnswer">Security quesiton answer for password.</param>
        /// <param name="isApproved">User is approved.</param>
        /// <param name="providerUserKey"></param>
        /// <param name="status"></param>
        /// <returns>MembershipUser</returns>
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException("password");
            if (RequiresUniqueEmail && string.IsNullOrEmpty(email)) throw new ArgumentNullException("email");

            var args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);

            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if ((RequiresUniqueEmail && !string.IsNullOrEmpty(email) && (GetUserNameByEmail(email) != string.Empty)))
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            var membershipUser = GetUser(username, false);

            if (membershipUser == null)
            {
                var salt = GenerateSalt();

                var membership = new Membership
                {
                    UserName = username,
                    Password = EncodePassword(password, salt),
                    Salt = salt,
                    Email = email,
                    IsApproved = true,
                    CreateDate = DateTime.UtcNow
                };

                try
                {
                    SaveUser(membership);
                    status = MembershipCreateStatus.Success;
                }
                catch (Exception ex)
                {
                    throw new MemberAccessException("Error processing membership data - " + ex.Message);
                }

                return GetUser(username, false);
            }

            status = MembershipCreateStatus.DuplicateUserName;

            return null;
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="deleteAllRelatedData">Whether to delete all related data.</param>
        /// <returns>T/F if the user was deleted.</returns>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            try
            {
                var u = GetUserByName(username);

                if (u != null)
                    DeleteUser(u);
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Get a collection of users.
        /// </summary>
        /// <param name="pageIndex">Page index.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="totalRecords">Total # of records to retrieve.</param>
        /// <returns>Collection of MembershipUser objects.</returns>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();
            totalRecords = 0;

            try
            {
                var uList = GetAllUsers(pageIndex, pageSize);

                foreach (var membership in uList)
                    users.Add(GetUserFromObject(membership));
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            totalRecords = users.Count;
            return users;
        }

        /// <summary>
        /// Gets the number of users currently on-line.
        /// </summary>
        /// <returns># of users on-line.</returns>
        public override int GetNumberOfUsersOnline()
        {
            var onlineSpan = new TimeSpan(0, System.Web.Security.Membership.UserIsOnlineTimeWindow, 0);
            var compareTime = DateTime.UtcNow.Subtract(onlineSpan);

            try
            {
                return GetNumberOfUsersOnline(compareTime);
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }
        }

        /// <summary>
        /// Get the password for a user.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="answer">Answer to security question.</param>
        /// <returns>Password for the user.</returns>
        public override string GetPassword(string username, string answer)
        {
            throw new ProviderException("Password Retrieval Not Available.");
        }

        /// <summary>
        /// Get a user record
        /// </summary>
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");

            MembershipUser membershipUser = null;

            try
            {
                var membership = GetUserByName(username);

                if (membership != null)
                {
                    if (userIsOnline)
                    {
                        membership.LastActivityDate = DateTime.UtcNow;
                        UpdateUser(membership);
                    }

                    membershipUser = GetUserFromObject(membership);
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Unable to retrieve user data - " + ex.Message);
            }

            return membershipUser;
        }

        /// <summary>
        /// Get a user based upon provider key and if they are on-line.
        /// </summary>
        /// <param name="userId">Provider key.</param>
        /// <param name="userIsOnline">T/F whether the user is on-line.</param>
        public override MembershipUser GetUser(object userId, bool userIsOnline)
        {
            MembershipUser membershipUser = null;

            try
            {
                var token = userId.ToString();
                var u = GetUserByToken(token);

                if (u != null)
                {
                    if (userIsOnline)
                    {
                        u.LastActivityDate = DateTime.UtcNow;
                        UpdateUser(u);
                    }

                    membershipUser = GetUserFromObject(u);
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            return membershipUser;
        }

        /// <summary>
        /// Unlock a user.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <returns>T/F if unlocked.</returns>
        public override bool UnlockUser(string username)
        {
            try
            {
                var u = GetUserByName(username);

                if (u != null)
                {
                    u.IsLockedOut = false;
                    UpdateUser(u);
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            return true;
        }

        public override string GetUserNameByEmail(string email)
        {
            try
            {
                return GetUserNameByEMail(email);
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }
        }

        /// <summary>
        /// Reset the user password.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="answer">Answer to security question.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public override string ResetPassword(string username, string answer)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException("username");

            if (!EnablePasswordReset)
                throw new NotSupportedException("Password Reset is not enabled.");

            var newPassword = System.Web.Security.Membership.GeneratePassword(8, 0);
            var args = new ValidatePasswordEventArgs(username, newPassword, false);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                if (args.FailureInformation != null)
                    throw args.FailureInformation;

                throw new MembershipPasswordException("Reset password canceled due to password validation failure.");
            }

            try
            {
                var u = GetUserByName(username);

                if (u.IsLockedOut)
                    throw new MembershipPasswordException("The supplied user is locked out.");

                u.Password = newPassword;
                UpdateUser(u);

                return newPassword;
            }
            catch (Exception ex)
            {
                throw new MembershipPasswordException(ex.Message);
            }
        }

        /// <summary>
        /// Update the user information.
        /// </summary>
        /// <param name="membershipUser">MembershipUser object containing data.</param>
        public override void UpdateUser(MembershipUser membershipUser)
        {
            try
            {
                var u = GetUserByName(membershipUser.UserName);
                u.Email = membershipUser.Email;
                u.IsApproved = membershipUser.IsApproved;
                UpdateUser(u);
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }
        }

        /// <summary>
        /// Validate the user based upon username and password.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>T/F if the user is valid.</returns>
        public override bool ValidateUser(string username, string password)
        {
            bool isValid = false;

            try
            {
                var u = GetUserByName(username);

                if (u != null)
                {
                    var storedPassword = u.Password;
                    var storedSalt = u.Salt;
                    var isApproved = u.IsApproved;

                    if (CheckPassword(password, storedPassword, storedSalt))
                    {
                        if (isApproved)
                        {
                            isValid = true;
                            u.LastLoginDate = DateTime.UtcNow;
                            UpdateUser(u);
                        }
                    }
                    else
                    {
                        UpdateFailureCount(username, FailureType.Password);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            return isValid;
        }

        /// <summary>
        /// Find all users matching a search string.
        /// </summary>
        /// <param name="usernameToMatch">Search string of user name to match.</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords">Total records found.</param>
        /// <returns>Collection of MembershipUser objects.</returns>
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();
            totalRecords = 0;

            try
            {
                var u = GetUserByName(usernameToMatch);

                if (u != null)
                    users.Add(GetUserFromObject(u));
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            totalRecords = users.Count;

            return users;
        }

        /// <summary>
        /// Find all users matching a search string of their email.
        /// </summary>
        /// <param name="emailToMatch">Search string of email to match.</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords">Total records found.</param>
        /// <returns>Collection of MembershipUser objects.</returns>
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var users = new MembershipUserCollection();
            totalRecords = 0;

            try
            {
                var username = GetUserNameByEmail(emailToMatch);

                if (username != null)
                {
                    var u = GetUserByName(username);
                    users.Add(GetUserFromObject(u));
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }

            totalRecords = users.Count;
            return users;
        }

        #endregion

        #region Utility Functions
        /// <summary>
        /// Create a MembershipUser object from a user object.
        /// </summary>
        /// <param name="membership">User Object.</param>
        /// <returns>MembershipUser object.</returns>
        private MembershipUser GetUserFromObject(Membership membership)
        {
            if (membership == null) throw new ArgumentNullException("membership");

            var creationDate = membership.CreateDate;
            var lastLoginDate = membership.LastLoginDate ?? DateTime.UtcNow;
            var lastActivityDate = membership.LastActivityDate ?? DateTime.UtcNow;
            var lastPasswordChangedDate = membership.LastPasswordChangedDate ?? DateTime.UtcNow;
            var lastLockedOutDate = membership.LastLockedOutDate ?? DateTime.UtcNow;

            return new MembershipUser(
                ProviderName,
                membership.UserName,
                membership.HashKey,
                membership.Email ?? string.Empty,
                string.Empty,
                string.Empty,
                membership.IsApproved,
                membership.IsLockedOut,
                creationDate,
                lastLoginDate,
                lastActivityDate,
                lastPasswordChangedDate,
                lastLockedOutDate
            );
        }

        /// <summary>
        /// Update password and answer failure information.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="failureType">Type of failure</param>
        /// <remarks></remarks>
        private void UpdateFailureCount(string username, FailureType failureType)
        {
            try
            {
                var u = GetUserByName(username);

                if (u != null)
                {
                    if (failureType == FailureType.Password) u.FailedPasswordAttemptCount++;
                    else u.FailedPasswordAnswerAttemptCount++;

                    MembershipLogic.UpdateUser(u);
                }
            }
            catch (Exception ex)
            {
                throw new MemberAccessException("Error processing membership data - " + ex.Message);
            }
        }

        private void MembershipProviderValidatingPassword(object sender, ValidatePasswordEventArgs e)
        {
            //Enforce our criteria
            var errorMessage = "";
            var pwChar = e.Password.ToCharArray();

            //Check Length
            if (e.Password.Length < _minRequiredPasswordLength)
            {
                errorMessage += "[Minimum length: " + _minRequiredPasswordLength + "]";
                e.Cancel = true;
            }

            //Check Strength
            if (_passwordStrengthRegularExpression != string.Empty)
            {
                var r = new Regex(_passwordStrengthRegularExpression);

                if (!r.IsMatch(e.Password))
                {
                    errorMessage += "[Insufficient Password Strength]";
                    e.Cancel = true;
                }
            }

            //Check Non-alpha characters
            var iNumNonAlpha = pwChar.Count(c => !char.IsLetterOrDigit(c));

            if (iNumNonAlpha < _minRequiredNonAlphanumericCharacters)
            {
                errorMessage += "[Insufficient Non-Alpha Characters]";
                e.Cancel = true;
            }

            e.FailureInformation = new MembershipPasswordException(errorMessage);
        }

        /// <summary>
        /// Check the password format based upon the MembershipPasswordFormat.
        /// </summary>
        /// <param name="password">Password</param>
        /// <param name="dbpassword">Password stored in the database.</param>
        /// <param name="dbsalt">Password salt stored in the database.</param>
        private bool CheckPassword(string password, string dbpassword, string dbsalt)
        {
            var pass1 = password;
            var pass2 = dbpassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Encrypted:
                    pass2 = UnEncodePassword(dbpassword);
                    break;

                case MembershipPasswordFormat.Hashed:
                    pass1 = EncodePassword(password, dbsalt);
                    break;

                default:
                    break;
            }

            return pass1 == pass2;
        }

        /// <summary>
        /// Encode password.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <param name="salt">Password salt.</param>
        /// <returns>Encoded password.</returns>
        private string EncodePassword(string password, string salt)
        {
            if (password == null) return null;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    return password;

                case MembershipPasswordFormat.Encrypted:
                    return Convert.ToBase64String(EncryptPassword(Encoding.Unicode.GetBytes(password)));

                case MembershipPasswordFormat.Hashed:
                    {
                        if (string.IsNullOrEmpty(salt))
                            throw new ArgumentException("Salt is required for encoding hashed passwords.", "salt");

                        var bytes = Encoding.Unicode.GetBytes(password);
                        var src = Convert.FromBase64String(salt);
                        var dst = new byte[src.Length + bytes.Length];

                        Buffer.BlockCopy(src, 0, dst, 0, src.Length);
                        Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);

                        var algorithm = HashAlgorithm.Create(PasswordHashAlgorithm);
                        var inArray = algorithm.ComputeHash(dst);

                        return Convert.ToBase64String(inArray);
                    }

                default:
                    throw new ProviderException("Unsupported password format.");
            }
        }

        /// <summary>
        /// UnEncode password.
        /// </summary>
        /// <param name="encodedPassword">Password.</param>
        /// <returns>Unencoded password.</returns>
        private string UnEncodePassword(string encodedPassword)
        {
            var password = encodedPassword;

            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Clear:
                    break;

                case MembershipPasswordFormat.Encrypted:
                    password = Encoding.Unicode.GetString(DecryptPassword(Convert.FromBase64String(password)));
                    break;

                case MembershipPasswordFormat.Hashed:
                    throw new ProviderException("Cannot unencode a hashed password.");

                default:
                    throw new ProviderException("Unsupported password format.");
            }

            return password;
        }

        /// <summary>
        /// Generate new password salt.
        /// </summary>
        private string GenerateSalt()
        {
            switch (PasswordFormat)
            {
                case MembershipPasswordFormat.Hashed:
                    {
                        var saltBytes = new byte[16];
                        var rng = new RNGCryptoServiceProvider();
                        rng.GetNonZeroBytes(saltBytes);

                        return Convert.ToBase64String(saltBytes);
                    }

                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
