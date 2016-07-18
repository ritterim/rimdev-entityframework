using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.Data.SqlLocalDb;
using System.Linq;
using Xunit;

namespace RimDev.EntityFramework.Test
{
    public class MultipleResultSetsTests : IDisposable
    {
        public MultipleResultSetsTests()
        {
            instance = TemporarySqlLocalDbInstance.Create(true);

            instance.Start();
        }

        private readonly TemporarySqlLocalDbInstance instance;

        public void Dispose()
        {
            instance.Dispose();
        }

        [Fact]
        public void Can_execute_sql()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                var results = context
                    .MultipleResultsUsingSql("select * from users; select * from cars;")
                    .With<User>()
                    .With<Car>()
                    .Execute();

                Assert.NotNull(results);
                Assert.Equal(2, results.Count);
                Assert.Equal(typeof(User), results[0].GetType().GetGenericArguments()[0]);
                Assert.Equal(typeof(Car), results[1].GetType().GetGenericArguments()[0]);
            }
        }

        [Fact]
        public void Can_execute_sql_with_out_parameter()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                IEnumerable<User> users = null;
                IEnumerable<Car> cars = null;

                context
                    .MultipleResultsUsingSql(
@"
set identity_insert users on;
insert into users (id) values (1);
set identity_insert users off;

set identity_insert cars on;
insert into cars (id) values (1);
set identity_insert cars off;

select * from users;
select * from cars;")
                    .With<User>(out users)
                    .With<Car>(out cars)
                    .Execute();

                Assert.NotNull(users);
                Assert.NotNull(cars);
                Assert.Equal(1, users.Count());
                Assert.Equal(1, cars.Count());
            }
        }

        [Fact]
        public void Can_execute_sql_with_parameters()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                IEnumerable<User> users = null;
                IEnumerable<Car> cars = null;

                context
                    .MultipleResultsUsingSql(
@"
set identity_insert users on;
insert into users (id) values (1);
set identity_insert users off;

set identity_insert cars on;
insert into cars (id) values (1);
set identity_insert cars off;

select * from users where id = @carId;
select * from cars where id = @userId;",
parameters: new List<SqlParameter>()
{
    new SqlParameter("userId", 2),
    new SqlParameter("carId", 2)
})
                    .With<User>(out users)
                    .With<Car>(out cars)
                    .Execute();

                Assert.NotNull(users);
                Assert.NotNull(cars);
                Assert.Equal(0, users.Count());
                Assert.Equal(0, cars.Count());
            }
        }

        [Fact]
        public void Can_execute_stored_procedure()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                context.Database.ExecuteSqlCommand(
@"create procedure getAllTheThings
as
begin
    select * from users;
    select * from cars;
end;");
                var results = context
                    .MultipleResultsUsingStoredProcedure("getAllTheThings")
                    .With<User>()
                    .With<Car>()
                    .Execute();

                Assert.NotNull(results);
                Assert.Equal(2, results.Count);
                Assert.Equal(typeof(User), results[0].GetType().GetGenericArguments()[0]);
                Assert.Equal(typeof(Car), results[1].GetType().GetGenericArguments()[0]);
            }
        }

        [Fact]
        public void Can_execute_stored_procedure_with_out_parameter()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                IEnumerable<User> users = null;
                IEnumerable<Car> cars = null;

                context.Database.ExecuteSqlCommand(
@"create procedure getAllTheThings
as
begin
    set identity_insert users on;
    insert into users (id) values (1);
    set identity_insert users off;

    set identity_insert cars on;
    insert into cars (id) values (1);
    set identity_insert cars off;

    select * from users;
    select * from cars;
end;");
                var results = context
                    .MultipleResultsUsingStoredProcedure("getAllTheThings")
                    .With<User>(out users)
                    .With<Car>(out cars)
                    .Execute();

                Assert.NotNull(users);
                Assert.NotNull(cars);
                Assert.Equal(1, users.Count());
                Assert.Equal(1, cars.Count());
            }
        }

        [Fact]
        public void Can_execute_stored_procedure_with_parameters()
        {
            using (var context = new TestDbContext(instance.CreateConnectionStringBuilder().ConnectionString))
            {
                IEnumerable<User> users = null;
                IEnumerable<Car> cars = null;

                context.Database.ExecuteSqlCommand(
@"create procedure getAllTheThings
(
    @userId int,
    @carId int
)
as
begin
    set identity_insert users on;
    insert into users (id) values (1);
    set identity_insert users off;

    set identity_insert cars on;
    insert into cars (id) values (1);
    set identity_insert cars off;

    select * from users where id = @userId;
    select * from cars where id = @carId;
end;");
                var results = context
                    .MultipleResultsUsingStoredProcedure("getAllTheThings", parameters:
                    new List<SqlParameter>()
                    {
                        new SqlParameter("userId", 2),
                        new SqlParameter("carId", 2)
                    })
                    .With<User>(out users)
                    .With<Car>(out cars)
                    .Execute();

                Assert.NotNull(users);
                Assert.NotNull(cars);
                Assert.Equal(0, users.Count());
                Assert.Equal(0, cars.Count());
            }
        }

        private class TestDbContext : DbContext
        {
            public TestDbContext(string connection)
                : base(connection)
            {
                Database.SetInitializer<TestDbContext>(new MigrateDatabaseToLatestVersion<TestDbContext, Configuration>(useSuppliedContext: true));

                Database.Initialize(force: true);
            }

            public DbSet<User> Users { get; set; }
            public DbSet<Car> Cars { get; set; }
        }

        private class Configuration : DbMigrationsConfiguration<TestDbContext>
        {
            public Configuration()
            {
                AutomaticMigrationsEnabled = true;
            }
        }

        private class User
        {
            public int Id { get; set; }
        }

        private class Car
        {
            public int Id { get; set; }
        }
    }
}
