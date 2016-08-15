using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Data;
using Dapper;
using Npgsql;

namespace AspNet.Identity.PG
{
    public class UserStore<TUser> :
    UserStore<TUser, IdentityRole, int, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>,
    IUserStore<TUser, int> where TUser : IdentityUser
    {

        public UserStore(string dbConnectionString)
            : base(dbConnectionString)
        {
        }
    }
    public class UserStore<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> :
        IUserLoginStore<TUser, TKey>,
        IUserClaimStore<TUser, TKey>,
        IUserRoleStore<TUser, TKey>,
        IUserPasswordStore<TUser, TKey>,
        IUserSecurityStampStore<TUser, TKey>,
        IUserEmailStore<TUser, TKey>,
        IUserPhoneNumberStore<TUser, TKey>,
        IUserTwoFactorStore<TUser, TKey>,
        IUserLockoutStore<TUser, TKey>
        where TKey : IEquatable<TKey>
        where TUser : IdentityUser<TKey, TUserLogin, TRole, TUserClaim>
        where TRole : IdentityRole
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
    {
        protected string _connectionString;
        private IDbConnection _connection;
        public UserStore(string dbConnectionString)
        {
            _connectionString = dbConnectionString;
        }


        public async Task AddClaimAsync(TUser user, Claim claim)
        {
            await AddClaimAsync(user, claim, null);           
        }
        private async Task AddClaimAsync(TUser user, Claim claim, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (claim == null)
                {
                    throw new ArgumentNullException("claim");
                }
                string sql = "insert into aspnetuserclaims(id, userid, claimtype,claimvalue) values(default, @userId, @claimtype, @claimvalue);";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { userId = user.Id, claimtype = claim.Type, claimvalue = claim.Value }, trans);
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }                
            }

        }
        public async Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            await AddLoginAsync(user, login, null);
        }

        private async Task AddLoginAsync(TUser user, UserLoginInfo login, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (login == null)
                {
                    throw new ArgumentNullException("login");
                }
                string sql = "insert into aspnetuserlogins(loginprovider, providerkey, userid) values(@loginprovider, @providerkey, @userid);";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { loginprovider = login.LoginProvider, providerkey = login.ProviderKey, userId = user.Id }, trans);
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task AddToRoleAsync(TUser user, string roleName)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (string.IsNullOrEmpty(roleName) == true)
                {
                    throw new ArgumentException("roleName must be set.");
                }
                OpenConnection();
                string roleSql = "select * from aspnetroles where name = @rolename";
                var role = await _connection.QuerySingleAsync<TRole>(roleSql, new { rolename = roleName });
                if (role == null)
                {
                    throw new InvalidOperationException("Role not found.");
                }
                await AddToRoleAsync(user, role, null);
            }
            finally
            {
                CloseConnection();
            }
        }

        private async Task AddToRoleAsync(TUser user, TRole role, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (role == null)
                {
                    throw new ArgumentNullException("role");
                }
                OpenConnection();
                string insertSql = "insert into aspnetuserroles(userid, roleid) values(@userid, @roleid);";
                await _connection.ExecuteAsync(insertSql, new { userId = user.Id, roleid = role.Id }, trans);
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task CreateAsync(TUser user)
        {
            IDbTransaction trans = null;
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }

                OpenConnection();
                trans = _connection.BeginTransaction();

                string insertSql = @"insert into aspnetusers(id, email, emailconfirmed, passwordhash, 
                                    securitystamp, phonenumber, phonenumberconfirmed, twofactorenabled,
                                    lockoutenddateutc, lockoutenabled,accessfailedcount,username,clientid) 
                                    values(default, @email, @emailconfirmed, @passwordhash, 
                                    @securitystamp, @phonenumber, @phonenumberconfirmed, @twofactorenabled,
                                    @lockoutenddateutc, @lockoutenabled, @accessfailedcount, @username, @clientid) returning id;";

                var userId = await _connection.QuerySingleAsync<TKey>(insertSql, new {
                    email = user.Email,
                    emailconfirmed = user.EmailConfirmed,
                    passwordhash = user.PasswordHash,
                    securitystamp = user.SecurityStamp,
                    phonenumber = user.PhoneNumber,
                    phonenumberconfirmed = user.PhoneNumberConfirmed,
                    twofactorenabled = user.TwoFactorEnabled,
                    lockoutenddateutc = user.LockoutEndDateUtc,
                    lockoutenabled = user.LockoutEnabled,
                    accessfailedcount = user.AccessFailedCount,
                    username = user.UserName,
                    clientid = user.ClientID
                });
                user.Id = userId;

                foreach (TUserClaim claim in user.Claims)
                {
                    var claimInfo = new Claim(claim.ClaimType, claim.ClaimValue);
                    await AddClaimAsync(user, claimInfo, trans);
                }
                foreach (TUserLogin login in user.Logins)
                {
                    var loginInfo = new UserLoginInfo(login.LoginProvider, login.ProviderKey);
                    await AddLoginAsync(user, loginInfo, trans);
                }
                foreach (TRole role in user.Roles)
                {
                    await AddToRoleAsync(user, role, trans);
                }
                trans.Commit();
                trans.Dispose();
                trans = null;
            }
            catch
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                throw;
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task DeleteAsync(TUser user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                OpenConnection();
                string sql = "delete from aspnetusers where id = @userid;";
                await _connection.ExecuteAsync(sql, new { userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing)
        {
            if (disposing == true &&
                _connection != null)
            {
                _connection.Dispose();
            }
        }

        public async Task<TUser> FindAsync(UserLoginInfo login)
        {
            try
            {
                if (login == null)
                {
                    throw new ArgumentNullException("login");
                }
                OpenConnection();
                string loginSql = "select * from aspnetuserlogins where loginprovider = @provider and providerkey = @key;";
                var loginInfo = await _connection.QuerySingleAsync<TUserLogin>(loginSql, new { provider = login.LoginProvider, key = login.ProviderKey });
                if (loginInfo == null)
                {
                    return null;
                }
                var user = await GetIdentityUserAsync(loginInfo.UserId);
                return user;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task<TUser> FindByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email) == true)
            {
                throw new ArgumentException("email must be set.");
            }
            OpenConnection();
            string sql = "select id from aspnetusers where lower(email) = @email;";
            var userId = await _connection.ExecuteScalarAsync<TKey>(sql, new { email = email.ToLower()});
            var user = await GetIdentityUserAsync(userId);
            return user;
        }

        public async Task<TUser> FindByIdAsync(TKey userId)
        {
            var user = await GetIdentityUserAsync(userId);
            return user;
        }

        public async Task<TUser> FindByNameAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName) == true)
            {
                throw new ArgumentException("userName must be set.");
            }
            OpenConnection();
            string sql = "select id from aspnetusers where lower(username) = @userName;";
            var userId = await _connection.ExecuteScalarAsync<TKey>(sql, new { userName = userName.ToLower() });
            var user = await GetIdentityUserAsync(userId);
            return user;
        }

        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.AccessFailedCount);
        }

        public async Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            List<Claim> claims = null;
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (user.Claims == null ||
                    user.Claims.Count == 0)
                {
                    OpenConnection();
                    string sql = "select * from aspnetuserclaims where userid = @Id;";

                    IEnumerable<IdentityUserClaim> userClaims;
                    userClaims = await _connection.QueryAsync<IdentityUserClaim>(sql, new { Id = user.Id });
                    claims = userClaims.Select(u => new Claim(u.ClaimType, u.ClaimValue)).ToList();                    
                }
                return claims;
            }
            finally
            {
                CloseConnection();
            }
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            DateTimeOffset lockoutDateTime = DateTimeOffset.MinValue;
            if (user.LockoutEndDateUtc.HasValue == true)
            {
                lockoutDateTime = DateTime.SpecifyKind(user.LockoutEndDateUtc.Value, DateTimeKind.Utc);
                
            }            
            return Task.FromResult(lockoutDateTime);
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            List<UserLoginInfo> logins = null;
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (user.Logins == null ||
                    user.Logins.Count == 0)
                {
                    OpenConnection();
                    string sql = "select * from aspnetuserlogins where userid = @Id;";

                    IEnumerable<IdentityUserLogin> userLogins;
                    userLogins = await _connection.QueryAsync<IdentityUserLogin>(sql, new { Id = user.Id });
                    logins = userLogins.Select(u => new UserLoginInfo(u.LoginProvider, u.ProviderKey)).ToList();
                }
                return logins;
            }
            finally
            {
                CloseConnection();
            }
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public async Task<IList<string>> GetRolesAsync(TUser user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (user.Roles == null ||
                    user.Roles.Count == 0)
                {
                    OpenConnection();
                    string sql = @"select ar.* from aspnetroles ar 
                        inner join aspnetuserroles aur
                        on ar.id = aur.roleid
                        where aur.userid = @Id; ";
                    var roles = await _connection.QueryAsync<TRole>(sql, new { Id = user.Id });
                    user.Roles = roles.ToList();
                }
                return (user.Roles.Select(r => r.Name)).ToList();
            }
            finally
            {
                CloseConnection();
            }
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (user.SecurityStamp == null)
            {
                return Task.FromResult("");
            }
            else
            {
                return Task.FromResult(user.SecurityStamp);
            }
            
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public async Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                user.AccessFailedCount = user.AccessFailedCount + 1;
                string sql = "update aspnetusers set accessfailedcount = @newCount where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { newCount = user.AccessFailedCount, userId = user.Id});
                return user.AccessFailedCount;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrEmpty(roleName) == true)
            {
                throw new ArgumentException("roleName must be set.");
            }
            var roles = await GetRolesAsync(user);
            return roles.Contains(roleName);
        }

        public async Task RemoveClaimAsync(TUser user, Claim claim)
        {
            await RemoveClaimAsync(user, claim, null);
        }
        private async Task RemoveClaimAsync(TUser user, Claim claim, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (claim == null)
                {
                    throw new ArgumentNullException("claim");
                }
                string sql = "delete from aspnetuserclaims where claimtype = @claimType and claimvalue = @claimValue and userid = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { claimType = claim.Type, claimValue = claim.Value, userId = user.Id }, trans);
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task RemoveFromRoleAsync(TUser user, string roleName)
        {
            await RemoveFromRoleAsync(user, roleName, null);
        }

        private async Task RemoveFromRoleAsync(TUser user, string roleName, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (string.IsNullOrEmpty(roleName) == true)
                {
                    throw new ArgumentException("roleName must be set.");
                }

                string sql = @"delete aur from aspnetuserroles 
                                inner join aspnetroles ar 
                                on aur.roleid = ar.id 
                                where ar.name = @roleName and userid = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { roleName = roleName, userId = user.Id }, null);
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            await RemoveLoginAsync(user, login, null);
        }
        public async Task RemoveLoginAsync(TUser user, UserLoginInfo login, IDbTransaction trans)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (login == null)
                {
                    throw new ArgumentNullException("login");
                }
                string sql = "delete from aspnetuserlogins where loginprovider = @provider and providerkey = @key and userid = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { provider = login.LoginProvider, key = login.ProviderKey, userId = user.Id }, trans);

            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }

        public async Task ResetAccessFailedCountAsync(TUser user)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set accessfailedcount = 0 where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new {userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetEmailAsync(TUser user, string email)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (string.IsNullOrEmpty(email) == true)
                {
                    throw new ArgumentException("email must be set.");
                }
                string sql = "update aspnetusers set email = @email where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { email = email, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set emailconfirmed = @confirmed where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { confirmed = confirmed, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set lockoutenabled = @enabled where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { enabled = enabled, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                DateTimeOffset? lockoutDateTime;
                if (lockoutEnd == DateTimeOffset.MinValue)
                {
                    lockoutDateTime = null;
                }
                else
                {
                    lockoutDateTime = lockoutEnd.UtcDateTime;
                }
                string sql = "update aspnetusers set lockoutenddateutc = @lockoutEnd where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { lockoutEnd = lockoutDateTime, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                if (string.IsNullOrEmpty(passwordHash) == true)
                {
                    throw new ArgumentException("passwordHash must be set.");
                }
                string sql = "update aspnetusers set passwordhash = @passwordHash where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { passwordHash = passwordHash, userId = user.Id });
                user.PasswordHash = passwordHash;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set phonenumber = @phoneNumber where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { phoneNumber = phoneNumber, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set phonenumberconfirmed = @confirmed where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { confirmed = confirmed, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetSecurityStampAsync(TUser user, string stamp)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set securitystamp = @stamp where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { stamp = stamp, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }
                string sql = "update aspnetusers set twofactorenabled = @enabled where id = @userId;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { enabled = enabled, userId = user.Id });
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task UpdateAsync(TUser user)
        {
            IDbTransaction trans = null;
            try
            {
                if (user == null)
                {
                    throw new ArgumentNullException("user");
                }

                OpenConnection();
                trans = _connection.BeginTransaction();

                string updateSql = @"update aspnetusers set email = @email, emailconfirmed = @emailconfirmed, 
                                    passwordhash = @passwordhash, securitystamp = @securitystamp, 
                                    phonenumber = @phonenumber, phonenumberconfirmed = @phonenumberconfirmed, 
                                    twofactorenabled = @twofactorenabled, lockoutenddateutc = @lockoutenddateutc, 
                                    lockoutenabled = @lockoutenabled, accessfailedcount = @accessfailedcount,
                                    username = @username 
                                    where id = @Id;";

                var userId = await _connection.QuerySingleAsync<int>(updateSql, new
                {
                    email = user.Email,
                    emailconfirmed = user.EmailConfirmed,
                    passwordhash = user.PasswordHash,
                    securitystamp = user.SecurityStamp,
                    phonenumber = user.PhoneNumber,
                    phonenumberconfirmed = user.PhoneNumberConfirmed,
                    twofactorenabled = user.TwoFactorEnabled,
                    lockoutenddateutc = user.LockoutEndDateUtc,
                    lockoutenabled = user.LockoutEnabled,
                    accessfailedcount = user.AccessFailedCount,
                    username = user.UserName,
                    Id = user.Id
                });
                var dbClaims = await GetClaimsAsync(user);
                var dbLogins = await GetLoginsAsync(user);
                var dbRoles = await GetRolesAsync(user);
                foreach (TUserClaim claim in user.Claims)
                {
                    if (dbClaims.Count(c => c.Type == claim.ClaimType && c.Value == claim.ClaimValue) == 0)
                    {
                        var claimInfo = new Claim(claim.ClaimType, claim.ClaimValue);
                        await AddClaimAsync(user, claimInfo, trans);
                    }                                       
                }
                foreach (Claim dbClaim in dbClaims)
                {
                    if (user.Claims.Count(c => c.ClaimType == dbClaim.Type && c.ClaimValue == dbClaim.Value) == 0)
                    {
                        await RemoveClaimAsync(user, dbClaim, trans);
                    }
                }
                foreach (TUserLogin login in user.Logins)
                {
                    if (dbLogins.Count(l => l.LoginProvider == login.LoginProvider && l.ProviderKey == login.ProviderKey) == 0)
                    {
                        var loginInfo = new UserLoginInfo(login.LoginProvider, login.ProviderKey);
                        await AddLoginAsync(user, loginInfo, trans);
                    }
                }
                foreach (UserLoginInfo dbLogin in dbLogins)
                {
                    if (user.Logins.Count(l => l.ProviderKey == dbLogin.ProviderKey && l.LoginProvider == dbLogin.LoginProvider) == 0)
                    {
                        await RemoveLoginAsync(user, dbLogin, trans);
                    }
                }
                foreach (TRole role in user.Roles)
                {
                    if (dbRoles.Count(r => r == role.Name) == 0)
                    {
                        await AddToRoleAsync(user, role, trans);
                    }
                    await AddToRoleAsync(user, role, trans);
                }
                foreach (string dbRole in dbRoles)
                {
                    if (user.Roles.Count(r => r.Name == dbRole) == 0)
                    {
                        await RemoveFromRoleAsync(user, dbRole, trans);
                    }
                }
                trans.Commit();
                trans.Dispose();
                trans = null;
            }
            catch
            {
                if (trans != null)
                {
                    trans.Rollback();
                }
                throw;
            }
            finally
            {
                if (trans == null)
                {
                    CloseConnection();
                }
            }
        }
        private async Task<TUser> GetIdentityUserAsync(TKey userId)
        {
            try
            {

                OpenConnection();
                string sql = @"select * from aspnetusers where id = @Id;
                    select * from aspnetuserlogins where userid = @Id;
                    select * from aspnetuserclaims where userid = @Id;
                    select ar.* from aspnetroles ar 
                        inner join aspnetuserroles aur 
                        on ar.id = aur.roleid
                        where aur.userid = @Id";

                TUser user;
                using (var multi = await _connection.QueryMultipleAsync(sql, new { Id = userId}))
                {
                    user = multi.Read<TUser>().FirstOrDefault();
                    if (user != null)
                    {
                        user.Logins = multi.Read<TUserLogin>().ToList();
                        user.Claims = multi.Read<TUserClaim>().ToList();
                        user.Roles = multi.Read<TRole>().ToList();
                    }
                }

                return user;
            }
            finally
            {
                CloseConnection();
            }
        }
        private void OpenConnection()
        {
            if (_connection == null)
            {
                _connection = new NpgsqlConnection(_connectionString);
            }
            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }
        private void CloseConnection()
        {
            if (_connection != null &&
                _connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }
    }
}
