using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

namespace AspNet.Identity.PG.Test
{
    [TestClass]
    public class UserStoreTest
    {
        private UserStore<IdentityUser> _store;
        public UserStoreTest()
        {            
            _store = new UserStore<IdentityUser>(PostgresHelper.ConnectionString);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task AddUserWithNullParamFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            await _store.CreateAsync(null);
        }
        [TestMethod]
        [ExpectedException(typeof(PostgresException))]
        public async Task AddUserWithBlankUserNameFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            IdentityUser user = new IdentityUser();
            user.PasswordHash = "1238903248093248023948032948";
            await _store.CreateAsync(user);
        }

        [TestMethod]
        [ExpectedException(typeof(PostgresException))]
        public async Task AddUserWithBlankPasswordFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            await _store.CreateAsync(user);
        }
        [TestMethod]        
        public async Task AddUserWithExistingUserNameViaManagerFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            var manager = new UserManager<IdentityUser, int>(_store);
            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            user.Email = "bob1@test.com";
            user.PasswordHash = "1238903248093248023948032948";
            await manager.CreateAsync(user);
            user = new IdentityUser();
            user.UserName = "test";
            user.Email = "bob2@test.com";
            user.PasswordHash = "1238903248093248023948032948";
            var result = await manager.CreateAsync(user);
            Assert.AreNotEqual(true, result.Succeeded);
        }
        [TestMethod]
        public async Task AddUserWithExistingEmailViaManagerFailsTest()
        {
            PostgresHelper.CreateIdentityTables();
            var manager = new UserManager<IdentityUser, int>(_store);
            manager.UserValidator = new UserValidator<IdentityUser, int>(manager) { RequireUniqueEmail = true };

            IdentityUser user = new IdentityUser();
            user.UserName = "bob1";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            await manager.CreateAsync(user);
            user = new IdentityUser();
            user.UserName = "bob2";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            var result = await manager.CreateAsync(user);
            Assert.AreNotEqual(true, result.Succeeded);
        }

        [TestMethod]
        public async Task GetUserbyNameCaseInsensitiveTest()
        {
            PostgresHelper.CreateIdentityTables();
            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            await _store.CreateAsync(user);

            var result = await _store.FindByNameAsync("test");
            Assert.IsNotNull(result);

            result = await _store.FindByNameAsync("TEST");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task GetUserbyEmailCaseInsensitiveTest()
        {

            PostgresHelper.CreateIdentityTables();

            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            await _store.CreateAsync(user);

            var result = await _store.FindByEmailAsync("test@test.com");
            Assert.IsNotNull(result);

            result = await _store.FindByEmailAsync("TesT@TesT.CoM");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task CreateFindByIDDeleteUserTest()
        {

            PostgresHelper.CreateIdentityTables();

            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            await _store.CreateAsync(user);
            var id = user.Id;
            user = null;
            user = await _store.FindByIdAsync(id);
            Assert.IsNotNull(user);

            await _store.DeleteAsync(user);

            user = null;

            user = await _store.FindByIdAsync(id);
            Assert.IsNull(user);
        }
        [TestMethod]
        public async Task CreateRoleAddToUserTest()
        {
            PostgresHelper.CreateIdentityTables();

            RoleStore<IdentityRole> roleStore = new RoleStore<IdentityRole>(PostgresHelper.ConnectionString);
            await roleStore.CreateAsync(new IdentityRole { Name = "test" });

            IdentityUser user = new IdentityUser();
            user.UserName = "test";
            user.PasswordHash = "1238903248093248023948032948";
            user.Email = "test@test.com";
            await _store.CreateAsync(user);
            var id = user.Id;
            user = await _store.FindByIdAsync(id);
            Assert.AreEqual(0, user.Roles.Count);
            await _store.AddToRoleAsync(user, "test");
            user = await _store.FindByIdAsync(id);
            Assert.AreEqual("test", user.Roles[0].Name);
        }
    }
}
