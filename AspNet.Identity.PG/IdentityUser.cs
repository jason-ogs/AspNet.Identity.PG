using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;

namespace AspNet.Identity.PG
{
    public class IdentityUser : IdentityUser<int, IdentityUserLogin, IdentityRole, IdentityUserClaim>
    {
    }
    public class IdentityUser<TKey, TLogin, TRole, TClaim> : IUser<TKey>
        where TLogin : IdentityUserLogin<TKey>
        where TRole : IdentityRole
        where TClaim : IdentityUserClaim<TKey>
    {
        public IdentityUser()
        {
            Roles = new List<TRole>();
            Claims = new List<TClaim>();
            Logins = new List<TLogin>();
        }
        public TKey Id { get; set; }
        public string UserName{ get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }        
        public int AccessFailedCount { get; set; }
        public List<TRole> Roles { get; set; }
        public List<TClaim> Claims { get; set; }
        public List<TLogin> Logins { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEndDateUtc { get; set; }
        public bool LockoutEnabled { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        //Optional, for multi-tenant scenarios
        public int ClientID { get; set; }

    }
}
