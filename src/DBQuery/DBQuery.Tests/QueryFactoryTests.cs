using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace DBQuery.Tests;

[TestFixture, Order(1)]
public class QueryFactoryTests
{
    [Test]
    public void CreateSqlServerFactory_FromConnection()
    {
        var conn = new SqlConnection();
        var factory = QueryFactory.GetFactoryForConnection(conn);

        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void CreateSqlServerFactory_FromFactory()
    {
        var dbFactory = SqlClientFactory.Instance;
        var factory = QueryFactory.GetQueryFactory(dbFactory);

        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void CreateSQLiteFactory_FromConnection()
    {
        var conn = new SqliteConnection();
        var factory = QueryFactory.GetFactoryForConnection(conn);

        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void CreateSQLiteFactory_FromFactory()
    {
        var dbFactory = SqliteFactory.Instance;
        var factory = QueryFactory.GetQueryFactory(dbFactory);

        Assert.That(factory, Is.Not.Null);
    }
}
