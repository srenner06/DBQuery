using DBQuery.Compilers;
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

    [Test]
    public void Contructor_CustomCompiler_SQLite()
    {
        var compiler = new SQLiteCompiler(SqliteFactory.Instance);
        var factory = new QueryFactory(compiler);

        Assert.Pass();
    }

    [Test]
    public void Contructor_CustomCompiler_SqlServer()
    {
        var compiler = new SQLiteCompiler(SqlClientFactory.Instance);
        var factory = new QueryFactory(compiler);

        Assert.Pass();
    }

    [Test]
    public void Contructor_CustomCompilerNull_ShouldThrow()
    {
        QueryCompiler compiler = null!;
        void action() { var factory = new QueryFactory(compiler); }

        Assert.That((Action)action, Throws.ArgumentNullException);
    }
}
