using Microsoft.AspNet.Identity;


namespace AspNet.Identity.PG
{
    public class IdentityRole : IdentityRole<int, IdentityUserRole>
    {
    }
    public class IdentityRole<TKey, TUserRole> : IRole<TKey> where TUserRole : IdentityUserRole<TKey>
    {
        public TKey Id { get; set; }
        public string Name { get; set; }
    }

}
