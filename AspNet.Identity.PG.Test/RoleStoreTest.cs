using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System.Threading.Tasks;

namespace AspNet.Identity.PG.Test
{
    [TestClass]
    public class RoleStoreTest
    {
        private RoleStore<IdentityRole> _store;
        public RoleStoreTest()
        {
            _store = new RoleStore<IdentityRole>(PostgresHelper.ConnectionString);
        }
        [TestMethod]
        [ExpectedException(typeof(PostgresException))]
        public async Task CreateRoleWithBlankNameFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            IdentityRole role = new IdentityRole();
            await _store.CreateAsync(role);
        }
        [TestMethod]
        public async Task CreateRoleWithExistingNameViaManagerFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            var manager = new RoleManager<IdentityRole, int>(_store);
            IdentityRole role = new IdentityRole();
            role.Name = "test";
            await manager.CreateAsync(role);
            role = new IdentityRole();
            role.Name = "test";
            var result = await manager.CreateAsync(role);
            Assert.AreNotEqual(true, result.Succeeded);
        }
        [TestMethod]
        public async Task CreateFindByIDDeleteRoleTest()
        {

            PostgresHelper.CreateIdentityTables();

            IdentityRole role = new IdentityRole();
            role.Name = "test";
            await _store.CreateAsync(role);
            var id = role.Id;
            role = null;
            role = await _store.FindByIdAsync(id);
            Assert.IsNotNull(role);

            await _store.DeleteAsync(role);

            role = null;

            role = await _store.FindByIdAsync(id);
            Assert.IsNull(role);
        }
    }
}
