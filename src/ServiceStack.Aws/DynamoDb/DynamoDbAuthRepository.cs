// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Aws.DynamoDb
{
    public class DynamoDbAuthRepository : DynamoDbAuthRepository<UserAuth, UserAuthDetails>, IUserAuthRepository
    {
        public DynamoDbAuthRepository(IPocoDynamo db, bool initSchema = false) : base(db, initSchema) { }
    }

    public class DynamoDbAuthRepository<TUserAuth, TUserAuthDetails> : IUserAuthRepository, IManageRoles, IClearable, IRequiresSchema
        where TUserAuth : class, IUserAuth
        where TUserAuthDetails : class, IUserAuthDetails
    {
        public IPocoDynamo Db { get; private set; }

        public bool LowerCaseUsernames { get; set; }

        public class UserIdUserAuthDetailsIndex : IGlobalIndex<TUserAuthDetails>
        {
            [HashKey]
            public string UserId { get; set; }

            [RangeKey]
            public string Provider { get; set; }

            public int UserAuthId { get; set; }

            public int Id { get; set; }
        }

        public class UsernameUserAuthIndex : IGlobalIndex<TUserAuth>
        {
            [HashKey]
            public string UserName { get; set; }

            [RangeKey]
            public int Id { get; set; }
        }

        public DynamoDbAuthRepository(IPocoDynamo db, bool initSchema=false)
        {
            this.Db = db;

            if (initSchema)
                InitSchema();
        }

        private void ValidateNewUser(IUserAuth newUser, string password)
        {
            newUser.ThrowIfNull("newUser");
            password.ThrowIfNullOrEmpty("password");

            if (newUser.UserName.IsNullOrEmpty() && newUser.Email.IsNullOrEmpty())
                throw new ArgumentNullException("UserName or Email is required");

            if (!newUser.UserName.IsNullOrEmpty())
            {
                if (!HostContext.GetPlugin<AuthFeature>().IsValidUsername(newUser.UserName))
                    throw new ArgumentException("UserName contains invalid characters", "UserName");
            }
        }

        private void AssertNoExistingUser(IUserAuth newUser, IUserAuth exceptForExistingUser = null)
        {
            if (newUser.UserName != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.UserName);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("User {0} already exists".Fmt(newUser.UserName));
            }
            if (newUser.Email != null)
            {
                var existingUser = GetUserAuthByUserName(newUser.Email);
                if (existingUser != null
                    && (exceptForExistingUser == null || existingUser.Id != exceptForExistingUser.Id))
                    throw new ArgumentException("Email {0} already exists".Fmt(newUser.Email));
            }
        }

        public IUserAuth CreateUserAuth(IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(newUser);

            string salt, hash;
            HostContext.Resolve<IHashProvider>().GetHashAndSaltString(password, out hash, out salt);

            Sanitize(newUser);

            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.DigestHa1Hash = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);
            newUser.CreatedDate = DateTime.UtcNow;
            newUser.ModifiedDate = newUser.CreatedDate;

            Db.PutItem((TUserAuth)newUser);

            newUser = DeSanitize(Db.GetItem<TUserAuth>(newUser.Id));
            return newUser;
        }

        //DynamoDb does not allow null hash keys on Global Indexes
        //Workaround by populating UserName with Email when null
        private void Sanitize(IUserAuth userAuth)
        {
            if (userAuth.UserName == null)
                userAuth.UserName = userAuth.Email;

            if (this.LowerCaseUsernames)
                userAuth.UserName = userAuth.UserName.ToLower();
        }

        private IUserAuth DeSanitize(TUserAuth userAuth)
        {
            if (userAuth.UserName != null && userAuth.UserName.Contains("@"))
            {
                if (userAuth.Email == null)
                    userAuth.Email = userAuth.UserName;

                userAuth.UserName = null;
            }

            return userAuth;
        }

        public virtual void LoadUserAuth(IAuthSession session, IAuthTokens tokens)
        {
            session.ThrowIfNull("session");

            var userAuth = GetUserAuth(session, tokens);
            LoadUserAuth(session, userAuth);
        }

        private void LoadUserAuth(IAuthSession session, IUserAuth userAuth)
        {
            session.PopulateSession(userAuth,
                GetUserAuthDetails(session.UserAuthId).ConvertAll(x => (IAuthTokens)x));
        }

        public virtual IUserAuth GetUserAuth(string userAuthId)
        {
            return DeSanitize(Db.GetItem<TUserAuth>(int.Parse(userAuthId)));
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            if (authSession == null)
                throw new ArgumentNullException("authSession");

            var userAuth = !authSession.UserAuthId.IsNullOrEmpty()
                ? Db.GetItem<TUserAuth>(int.Parse(authSession.UserAuthId))
                : authSession.ConvertTo<TUserAuth>();

            if (userAuth.Id == default(int) && !authSession.UserAuthId.IsNullOrEmpty())
                userAuth.Id = int.Parse(authSession.UserAuthId);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Db.PutItem(userAuth);
        }

        public void SaveUserAuth(IUserAuth userAuth)
        {
            if (userAuth == null)
                throw new ArgumentNullException("userAuth");

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Db.PutItem((TUserAuth)userAuth);
        }

        public List<IUserAuthDetails> GetUserAuthDetails(string userAuthId)
        {
            var id = int.Parse(userAuthId);
            return Db.Query(Db.FromQuery<TUserAuthDetails>(q => q.UserAuthId == id))
                .Cast<IUserAuthDetails>().ToList();
        }

        public IUserAuthDetails CreateOrMergeAuthSession(IAuthSession authSession, IAuthTokens tokens)
        {
            TUserAuth userAuth = (TUserAuth)GetUserAuth(authSession, tokens)
                ?? typeof(TUserAuth).CreateInstance<TUserAuth>();

            TUserAuthDetails authDetails = null;
            var index = GetUserAuthByProviderUserId(tokens.Provider, tokens.UserId);
            if (index != null)
                authDetails = Db.GetItem<TUserAuthDetails>(index.UserAuthId, index.Id);

            if (authDetails == null)
            {
                authDetails = typeof(TUserAuthDetails).CreateInstance<TUserAuthDetails>();
                authDetails.Provider = tokens.Provider;
                authDetails.UserId = tokens.UserId;
            }

            authDetails.PopulateMissing(tokens, overwriteReserved: true);
            userAuth.PopulateMissingExtended(authDetails);

            userAuth.ModifiedDate = DateTime.UtcNow;
            if (userAuth.CreatedDate == default(DateTime))
                userAuth.CreatedDate = userAuth.ModifiedDate;

            Sanitize(userAuth);

            Db.PutItem(userAuth);

            authDetails.UserAuthId = userAuth.Id;

            authDetails.ModifiedDate = userAuth.ModifiedDate;
            if (authDetails.CreatedDate == default(DateTime))
                authDetails.CreatedDate = userAuth.ModifiedDate;

            Db.PutItem(authDetails);

            return authDetails;
        }

        public IUserAuth GetUserAuth(IAuthSession authSession, IAuthTokens tokens)
        {
            if (!authSession.UserAuthId.IsNullOrEmpty())
            {
                var userAuth = GetUserAuth(authSession.UserAuthId);
                if (userAuth != null)
                    return userAuth;
            }
            if (!authSession.UserAuthName.IsNullOrEmpty())
            {
                var userAuth = GetUserAuthByUserName(authSession.UserAuthName);
                if (userAuth != null)
                    return userAuth;
            }

            if (tokens == null || tokens.Provider.IsNullOrEmpty() || tokens.UserId.IsNullOrEmpty())
                return null;

            var authProviderIndex = GetUserAuthByProviderUserId(tokens.Provider, tokens.UserId);
            if (authProviderIndex != null)
            {
                var userAuth = DeSanitize(Db.GetItem<TUserAuth>(authProviderIndex.UserAuthId));
                return userAuth;
            }
            return null;
        }

        private UserIdUserAuthDetailsIndex GetUserAuthByProviderUserId(string provider, string userId)
        {
            var oAuthProvider = Db.FromQueryIndex<UserIdUserAuthDetailsIndex>(
                    q => q.UserId == userId && q.Provider == provider)
                .Exec()
                .FirstOrDefault();

            return oAuthProvider;
        }

        public IUserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            if (userNameOrEmail == null)
                return null;

            if (LowerCaseUsernames)
                userNameOrEmail = userNameOrEmail.ToLower();

            var index = Db.FromQueryIndex<UsernameUserAuthIndex>(q => q.UserName == userNameOrEmail)
                .Exec().FirstOrDefault();
            if (index == null)
                return null;

            var userAuthId = index.Id;

            return DeSanitize(Db.GetItem<TUserAuth>(userAuthId));
        }

        public bool TryAuthenticate(string userName, string password, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(userName);
            if (userAuth == null)
                return false;

            if (HostContext.Resolve<IHashProvider>().VerifyHashString(password, userAuth.PasswordHash, userAuth.Salt))
            {
                this.RecordSuccessfulLogin(userAuth);
                return true;
            }

            this.RecordInvalidLoginAttempt(userAuth);

            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string privateKey, int nonceTimeOut, string sequence, out IUserAuth userAuth)
        {
            userAuth = GetUserAuthByUserName(digestHeaders["username"]);
            if (userAuth == null)
                return false;

            var digestHelper = new DigestAuthFunctions();
            if (digestHelper.ValidateResponse(digestHeaders, privateKey, nonceTimeOut, userAuth.DigestHa1Hash, sequence))
            {
                this.RecordSuccessfulLogin(userAuth);
                return true;
            }

            this.RecordInvalidLoginAttempt(userAuth);

            userAuth = null;
            return false;
        }

        public IUserAuth UpdateUserAuth(IUserAuth existingUser, IUserAuth newUser, string password)
        {
            ValidateNewUser(newUser, password);

            AssertNoExistingUser(newUser, existingUser);

            //DynamoDb does not allow null hash keys on Global Indexes
            //Workaround by populating UserName with Email when null
            if (newUser.UserName == null)
                newUser.UserName = newUser.Email;

            if (this.LowerCaseUsernames)
                newUser.UserName = newUser.UserName.ToLower();

            var hash = existingUser.PasswordHash;
            var salt = existingUser.Salt;
            if (password != null)
                HostContext.Resolve<IHashProvider>().GetHashAndSaltString(password, out hash, out salt);

            var digestHash = existingUser.DigestHa1Hash;
            if (password != null || existingUser.UserName != newUser.UserName)
                digestHash = new DigestAuthFunctions().CreateHa1(newUser.UserName, DigestAuthProvider.Realm, password);

            newUser.Id = existingUser.Id;
            newUser.PasswordHash = hash;
            newUser.Salt = salt;
            newUser.DigestHa1Hash = digestHash;
            newUser.CreatedDate = existingUser.CreatedDate;
            newUser.ModifiedDate = DateTime.UtcNow;

            Db.PutItem((TUserAuth)newUser);

            return newUser;
        }

        public void DeleteUserAuth(string userAuthId)
        {
            var userId = int.Parse(userAuthId);

            Db.DeleteItem<TUserAuth>(userAuthId);

            var userAuthDetails = Db.FromQuery<TUserAuthDetails>(x => x.UserAuthId == userId)
                .Select(x => x.Id)
                .Exec();
            Db.DeleteItems<TUserAuthDetails>(userAuthDetails.Map(x => x.Id));

            var userAuthRoles = Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userId)
                .Select(x => x.Id)
                .Exec();
            Db.DeleteItems<UserAuthRole>(userAuthRoles.Map(x => x.Id));
        }

        public void Clear()
        {
            Db.DeleteItems<TUserAuth>(Db.FromScan<TUserAuth>().ExecColumn(x => x.Id));

            var qDetails = Db.FromScan<TUserAuthDetails>().Select(x => new { x.UserAuthId, x.Id });
            Db.DeleteItems<TUserAuthDetails>(qDetails.Exec().Map(x => new DynamoId(x.UserAuthId, x.Id)));

            var qRoles = Db.FromScan<UserAuthRole>().Select(x => new { x.UserAuthId, x.Id });
            Db.DeleteItems<UserAuthRole>(qRoles.Exec().Map(x => new DynamoId(x.UserAuthId, x.Id)));
        }

        public virtual ICollection<string> GetRoles(string userAuthId)
        {
            var authId = int.Parse(userAuthId);
            return Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Role != null)
                .Exec()
                .Map(x => x.Role);
        }

        public virtual ICollection<string> GetPermissions(string userAuthId)
        {
            var authId = int.Parse(userAuthId);
            return Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Permission != null)
                .Exec()
                .Map(x => x.Permission);
        }

        public virtual bool HasRole(string userAuthId, string role)
        {
            if (role == null)
                throw new ArgumentNullException("role");

            if (userAuthId == null)
                return false;

            var authId = int.Parse(userAuthId);

            return Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Role == role)
                .Exec()
                .Any();
        }

        public virtual bool HasPermission(string userAuthId, string permission)
        {
            if (permission == null)
                throw new ArgumentNullException("permission");

            if (userAuthId == null)
                return false;

            var authId = int.Parse(userAuthId);

            return Db.FromQuery<UserAuthRole>(x => x.UserAuthId == authId)
                .Filter(x => x.Permission == permission)
                .Exec()
                .Any();
        }

        public virtual void AssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var userAuth = GetUserAuth(userAuthId);
            var now = DateTime.UtcNow;

            var userRoles = Db.FromQuery<UserAuthRole>(q => q.UserAuthId == userAuth.Id)
                .Exec().ToList();

            if (!roles.IsEmpty())
            {
                var roleSet = userRoles.Where(x => x.Role != null).Select(x => x.Role).ToHashSet();
                foreach (var role in roles)
                {
                    if (!roleSet.Contains(role))
                    {
                        Db.PutRelatedItem(userAuth.Id, new UserAuthRole
                        {
                            Role = role,
                            CreatedDate = now,
                            ModifiedDate = now,
                        });
                    }
                }
            }

            if (!permissions.IsEmpty())
            {
                var permissionSet = userRoles.Where(x => x.Permission != null).Select(x => x.Permission).ToHashSet();
                foreach (var permission in permissions)
                {
                    if (!permissionSet.Contains(permission))
                    {
                        Db.PutRelatedItem(userAuth.Id, new UserAuthRole
                        {
                            Permission = permission,
                            CreatedDate = now,
                            ModifiedDate = now,
                        });
                    }
                }
            }
        }

        public virtual void UnAssignRoles(string userAuthId, ICollection<string> roles = null, ICollection<string> permissions = null)
        {
            var userAuth = GetUserAuth(userAuthId);

            if (!roles.IsEmpty())
            {
                var authRoleIds = Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userAuth.Id)
                    .Filter(x => roles.Contains(x.Role))
                    .Select(x => new { x.UserAuthId, x.Id })
                    .Exec()
                    .Map(x => new DynamoId(x.UserAuthId, x.Id));

                Db.DeleteItems<UserAuthRole>(authRoleIds);
            }

            if (!permissions.IsEmpty())
            {
                var authRoleIds = Db.FromQuery<UserAuthRole>(x => x.UserAuthId == userAuth.Id)
                    .Filter(x => permissions.Contains(x.Permission))
                    .Select(x => new { x.UserAuthId, x.Id })
                    .Exec()
                    .Map(x => new DynamoId(x.UserAuthId, x.Id));

                Db.DeleteItems<UserAuthRole>(authRoleIds);
            }
        }

        private bool hasInitSchema = false;

        public void InitSchema()
        {
            if (hasInitSchema)
                return;

            hasInitSchema = true;

            typeof(TUserAuth).AddAttributes(
                new ReferencesAttribute(typeof(UsernameUserAuthIndex)));

            Db.RegisterTable<TUserAuth>();

            typeof(TUserAuthDetails).AddAttributes(
                new ReferencesAttribute(typeof(UserIdUserAuthDetailsIndex)),
                new CompositeKeyAttribute("UserAuthId", "Id"));

            Db.RegisterTable<TUserAuthDetails>();

            typeof(UserAuthRole).AddAttributes(
                new CompositeKeyAttribute("UserAuthId", "Id"));

            Db.RegisterTable<UserAuthRole>();

            Db.InitSchema();
        }
    }

}