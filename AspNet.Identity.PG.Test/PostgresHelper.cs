using Npgsql;


namespace AspNet.Identity.PG.Test
{
    public static class PostgresHelper
    {
        public static readonly string ConnectionString = "Server=localhost;Port=5432;User ID=aspidtestuser;Password=1234!;Database=aspidtest";


        //DB Creation script
        //private static readonly string _dbCreateSql = @"create database aspidtest
        //                                                with owner = aspidtestuser
        //                                                encoding = 'UTF8';";

        private static readonly string _dropTablesSql = @"drop table if exists aspnetuserlogins;
                                                        drop table if exists aspnetuserclaims;
                                                        drop table if exists aspnetuserroles;
                                                        drop table if exists aspnetroles; 
                                                        drop table if exists aspnetusers;";

        private static readonly string _userTableCreateSql = @"create table aspnetusers(id serial not null,
                                                                username varchar(256) not null,
                                                                email varchar(256) not null,
                                                                emailconfirmed boolean not null,
                                                                passwordhash varchar(256) not null,
                                                                securitystamp varchar(256),
                                                                phonenumber varchar(256),
                                                                phonenumberconfirmed boolean not null,
                                                                twofactorenabled boolean not null,
                                                                lockoutenddateutc timestamp,
                                                                lockoutenabled boolean not null,
                                                                accessfailedcount integer not null,
                                                                clientid integer null,
                                                                constraint pk_aspnetusers primary key (id));";

        private static readonly string _roleTableCreateSql = @"create table aspnetroles(id serial not null,
                                                                name varchar(256) not null,
                                                                constraint pk_aspnetroles primary key (id));";

        private static readonly string _userRoleTableCreateSql = @"create table aspnetuserroles(userid integer not null,
                                                                    roleid integer not null,
                                                                    constraint pk_aspnetuserroles primary key (userid, roleid),
                                                                    constraint fk_aspnetuserroles_aspnetroles_roleid foreign key (roleid)
                                                                        references aspnetroles (id) match simple
                                                                            on update no action on delete cascade,
                                                                    constraint fk_aspnetuserroles_aspnetusers_userid foreign key (userid)
                                                                        references aspnetusers (id) match simple
                                                                            on update no action on delete cascade);";

        private static readonly string _claimTableCreateSql = @"create table aspnetuserclaims(id serial not null,
                                                              userid integer not null,
                                                              claimtype varchar(100),
                                                              claimvalue varchar(100),
                                                              constraint pk_aspnetuserclaims primary key (id),
                                                              constraint fk_aspnetuserclaims_aspnetusers_userid foreign key (userid)
                                                                  references aspnetusers (id) match simple
                                                                  on update no action on delete cascade);";

        private static readonly string _loginTableCreateSql = @"create table aspnetuserlogins(
                                                                userid integer not null,
                                                                loginprovider varchar(128),
                                                                providerkey varchar(128),
                                                                constraint pk_aspnetuserlogins primary key (userid,loginprovider,providerkey),
                                                                constraint fk_aspnetuserlogins_aspnetusers_userid foreign key (userid)
                                                                    references aspnetusers (id) match simple
                                                                    on update no action on delete cascade);";

        private static readonly string _insensitivequeryindexSql = @"create index on aspnetusers(lower(username));
                                                                    create index on aspnetusers(lower(email));";

        public static void CreateIdentityTables()
        {


            NpgsqlConnection conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            NpgsqlCommand cmd = new NpgsqlCommand(_dropTablesSql, conn);
            cmd.ExecuteNonQuery();

            cmd = new NpgsqlCommand(_userTableCreateSql, conn);
            cmd.ExecuteNonQuery();

            cmd.CommandText = _roleTableCreateSql;
            cmd.ExecuteNonQuery();

            cmd.CommandText = _userRoleTableCreateSql;
            cmd.ExecuteNonQuery();

            cmd.CommandText = _claimTableCreateSql;
            cmd.ExecuteNonQuery();

            cmd.CommandText = _loginTableCreateSql;
            cmd.ExecuteNonQuery();

            cmd.CommandText = _insensitivequeryindexSql;
            cmd.ExecuteNonQuery();

            conn.Close();
        }
    }
}
