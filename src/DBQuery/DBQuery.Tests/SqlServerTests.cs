using System.Data;
using DBQuery.QueryBuilders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace DBQuery.Tests;

[TestFixture]
public class SqlServerTests
{
    private MsSqlContainer _sqlContainer = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:latest")
            .Build();

        await _sqlContainer.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        await _sqlContainer.DisposeAsync();
    }

    #region Helper
    private SqlConnection OpenNewConnection()
    {
        var connString = _sqlContainer.GetConnectionString();
        var conn = new SqlConnection(connString);
        conn.Open();
        return conn;
    }

    private static string SetupNewTable(SqlConnection conn, int rows = 3)
    {
        var tableName = Guid.NewGuid().ToString();
        var c2 = conn.CreateCommand();
        c2.CommandText = $"Create Table [{tableName}] (col1 varchar(255),col2 varchar(255),col3 int)";
        c2.ExecuteNonQuery();

        if (rows == 0)
            return tableName;

        c2 = conn.CreateCommand();
        var rowsTexts = Enumerable.Range(1, rows)
            .Select(i => $"('val{i}', 'test{i}', {i})");

        c2.CommandText = $"Insert into [{tableName}] Values {string.Join(',', rowsTexts)}";
        c2.ExecuteNonQuery();
        return tableName;
    }
    #endregion

    [Test]
    public void Query_Columns()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetQueryFactory(SqlClientFactory.Instance);

        var cmd = helper.Select()
            .SetTable(tableName)
            .SetColumns("col1", "col3")
            .ToCommand() as SqlCommand;

        cmd!.Connection = conn;
        using var da = new SqlDataAdapter(cmd);

        var dt = new DataTable();
        da.Fill(dt);

        Assert.That(dt.Columns, Has.Count.EqualTo(2));
        Assert.That(dt.Columns[0].ColumnName, Is.EqualTo("col1"));
        Assert.That(dt.Columns[1].ColumnName, Is.EqualTo("col3"));
    }

    [Test]
    public void Query_Where_OneGroupAnd_ExpectNone()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetQueryFactory(SqlClientFactory.Instance);

        var cmd = helper.Select()
            .SetTable(tableName)
            .AddFilter("col1", "val1")
            .AddFilter("col2", "val2")
            .ToCommand() as SqlCommand;

        cmd!.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public void Query_Where_OneGroupOr_ExpectOne()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetQueryFactory(SqlClientFactory.Instance);

        var cmd = helper.Select()
            .SetTable(tableName)
            .AddFilter("col1", "val1")
            .AddFilter("col2", "val2", LogicalOperation.Or)
            .ToCommand() as SqlCommand;

        cmd!.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    [TestCase(5, 4, 4)]
    [TestCase(0, 4, 0)]
    [TestCase(1, 4, 1)]
    [TestCase(4, 4, 4)]
    [TestCase(50, 35, 35)]
    public void Query_Where_SmallerThan_ExpectMultiple(int rowsCount, int smallerThan, int expectedCount)
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rowsCount);

        var helper = QueryFactory.GetQueryFactory(SqlClientFactory.Instance);

        var cmd = helper.Select()
            .SetTable(tableName)
            .AddFilter("col3", smallerThan, op: "<=")
            .ToCommand() as SqlCommand;

        cmd!.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(expectedCount));
    }


    [Test]
    [TestCase(3, 2, 2)]
    [TestCase(3, 1, 1)]
    [TestCase(1, 2, 1)]
    [TestCase(30, 0, 30)]
    public void Query_WithLimit(int rowsCount, int limit, int expectedResult)
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rowsCount);

        var helper = QueryFactory.GetQueryFactory(SqlClientFactory.Instance);

        var cmd = helper.Select()
            .SetTable(tableName)
            .SetLimit(limit)
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(expectedResult));
    }

}
