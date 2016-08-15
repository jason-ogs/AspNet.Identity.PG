
namespace AspNet.Identity.PG
{
    public class IdentityUserClaim<TKey>
    {
        public virtual int Id { get; set; }

        public TKey UserId { get; set; }

        public string ClaimType { get; set; }

        public string ClaimValue { get; set; }
    }
    public class IdentityUserClaim : IdentityUserClaim<int>
    {
    }
}
