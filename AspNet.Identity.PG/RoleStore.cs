using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
namespace AspNet.Identity.PG
{

    public class RoleStore<TRole, TKey, TUserRole> : IQueryableRoleStore<TRole, TKey>
    where TUserRole : IdentityUserRole<TKey>
    where TRole : IdentityRole<TKey, TUserRole>
    {
        private string _connectionString;
        private IDbConnection _connection;

        public RoleStore(string dbConnectionString)
        {
            _connectionString = dbConnectionString;
        }
        public IQueryable<TRole> Roles
        {
            get
            {
                return GetAllRoles().AsQueryable();
            }
        }

        public async Task CreateAsync(TRole role)
        {
            try
            {
                if (role == null)
                {
                    throw new ArgumentNullException("role");
                }
                string sql = "insert into aspnetroles(id, name) values(default, @name) returning id;";
                OpenConnection();
                var roleId = await _connection.QuerySingleAsync<TKey>(sql, new { name = role.Name });
                role.Id = roleId;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task DeleteAsync(TRole role)
        {
            try
            {
                if (role == null)
                {
                    throw new ArgumentNullException("role");
                }
                OpenConnection();
                string sql = "delete from aspnetroles where id = @roleId;";
                await _connection.ExecuteAsync(sql, new { roleId = role.Id });
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
            if (disposing == true)
            {
                _connection.Dispose();
            }
        }

        public async Task<TRole> FindByIdAsync(TKey roleId)
        {
            try
            {
                OpenConnection();
                string sql = "select * from aspnetroles where id = @roleId;";

                TRole role;
                role = await _connection.QueryFirstOrDefaultAsync<TRole>(sql, new { roleId = roleId });                                
                return role;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task<TRole> FindByNameAsync(string roleName)
        {
            try
            {
                OpenConnection();
                string sql = "select * from aspnetroles where name = @roleName;";

                TRole role;
                role = await _connection.QueryFirstOrDefaultAsync<TRole>(sql, new { roleName = roleName });
                return role;
            }
            finally
            {
                CloseConnection();
            }
        }

        public async Task UpdateAsync(TRole role)
        {
            try
            {
                if (role == null)
                {
                    throw new ArgumentNullException("role");
                }
                string sql = "update aspnetroles set name = @name where id = @Id;";
                OpenConnection();
                await _connection.ExecuteAsync(sql, new { name = role.Name, Id = role.Id });
            }
            finally
            {
                CloseConnection();
            }
        }
        private IEnumerable<TRole> GetAllRoles()
        {
            try
            {
                OpenConnection();
                string sql = "select * from aspnetroles;";

                IEnumerable<TRole> roles;
                roles = _connection.Query<TRole>(sql);
                return roles;
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
    public class RoleStore<TRole> : RoleStore<TRole, int, IdentityUserRole>, IQueryableRoleStore<TRole, int>
    where TRole : IdentityRole
    {
        public RoleStore(string dbConnectionString)
            : base(dbConnectionString)
        {
        }
    }
}
