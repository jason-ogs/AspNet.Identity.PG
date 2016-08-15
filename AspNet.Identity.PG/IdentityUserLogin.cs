namespace AspNet.Identity.PG
{
    public class IdentityUserLogin : IdentityUserLogin<int>
    {
        public IdentityUserLogin()
        {
        }
    }
    public class IdentityUserLogin<TKey>
    {
        public TKey UserId { get; set; }
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }        
    }
}
