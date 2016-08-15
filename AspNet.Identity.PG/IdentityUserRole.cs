
namespace AspNet.Identity.PG
{
    public class IdentityUserRole : IdentityUserRole<int>
    {
    }
    public class IdentityUserRole<TKey>
    {
        public TKey UserId { get; set; }
        public TKey RoleId { get; set; }
    }
}
