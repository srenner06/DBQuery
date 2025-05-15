using System.Data;
using System.Data.Common;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

namespace DBQuery.Tests;

[TestFixture(QueryFactory.SupportedDBTypes.SqlServer)]
[TestFixture(QueryFactory.SupportedDBTypes.Postgres)]
[TestFixture(QueryFactory.SupportedDBTypes.SQLite)]
public class SelectTests(QueryFactory.SupportedDBTypes type)
{
    private readonly QueryFactory.SupportedDBTypes _currentDBType = type;
    private IDatabaseContainer _container = null!;


    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        if (_currentDBType == QueryFactory.SupportedDBTypes.Postgres)
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:latest")
                .Build();
        }
        else if (_currentDBType == QueryFactory.SupportedDBTypes.SqlServer)
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
        else
            _container = new SqliteContainer();

        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardownAsync()
    {
        await _container.DisposeAsync();
    }

    #region Helper
    private DbConnection OpenNewConnection()
    {
        var connString = _container.GetConnectionString();

        DbConnection conn = _currentDBType switch
        {
            QueryFactory.SupportedDBTypes.Postgres => new Npgsql.NpgsqlConnection(),
            QueryFactory.SupportedDBTypes.SqlServer => new Microsoft.Data.SqlClient.SqlConnection(),
            QueryFactory.SupportedDBTypes.SQLite => new Microsoft.Data.Sqlite.SqliteConnection(),
            _ => throw new ArgumentException()
        };

        conn.ConnectionString = connString;
        conn.Open();
        return conn;
    }
    private static string GenTableName(int len = 20)
    {
        var r = new Random();
        const string a = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
        const string b = a + "0123456789";
        return a[r.Next(a.Length)] + new string(
            [.. new char[len - 1].Select(_ => b[r.Next(b.Length)])]
        );
    }
    private string SetupNewTable(DbConnection conn, int rows = 3, int numberOffset = 0)
    {
        var tableName = GenTableName();
        var c2 = conn.CreateCommand();

        var tableNameQuoted = _currentDBType switch
        {
            QueryFactory.SupportedDBTypes.Postgres => $"\"{tableName}\"",
            QueryFactory.SupportedDBTypes.SqlServer => $"[{tableName}]",
            QueryFactory.SupportedDBTypes.SQLite => $"\"{tableName}\"",
            _ => throw new ArgumentException()
        };

        if (_currentDBType == QueryFactory.SupportedDBTypes.SqlServer || _currentDBType == QueryFactory.SupportedDBTypes.Postgres)
            c2.CommandText = $"Create Table {tableNameQuoted} (col1 varchar(255),col2 varchar(255),col3 int, col4 text)";
        else if (_currentDBType == QueryFactory.SupportedDBTypes.SQLite)
            c2.CommandText = $"Create Table {tableNameQuoted} (col1 TEXT,col2 TEXT,col3 INTEGER, col4 TEXT)";

        c2.ExecuteNonQuery();

        if (rows == 0)
            return tableName;

        c2 = conn.CreateCommand();
        var rowsTexts = Enumerable.Range(1, rows)
            .Select(i => $"('val{i + numberOffset}', 'test{i + numberOffset}', {i + numberOffset}, NULL)");

        c2.CommandText = $"Insert into {tableNameQuoted} Values {string.Join(',', rowsTexts)}";
        c2.ExecuteNonQuery();
        return tableName;
    }
    #endregion

    [Test]
    public void Query_Columns()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .SetColumns("col1", "col3")
            .ToCommand();

        cmd!.Connection = conn;
        var dt = new DataTable();
        dt.Load(cmd.ExecuteReader());

        Assert.That(dt.Columns, Has.Count.EqualTo(2));
        Assert.That(dt.Columns[0].ColumnName, Is.EqualTo("col1"));
        Assert.That(dt.Columns[1].ColumnName, Is.EqualTo("col3"));
    }

    [Test]
    public void Query_Where_OneGroupAnd_ExpectNone()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .AddFilter("col1", (StringVal)"val1")
            .AddFilter("col2", "val2")
            .ToCommand();

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

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .StartGroup()
            .AddFilter("col1", "val1")
            .AddFilter("col2", "val2", LogicalOperator.Or)
            .EndGroup()
            .ToCommand();

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

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .And("col3", smallerThan, ValueOperator.SmallerEquals)
            .ToCommand();

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

        var helper = QueryFactory.GetFactoryForConnection(conn);

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


    [Test]
    [TestCase(3, 2, 1)]
    [TestCase(3, 1, 2)]
    [TestCase(1, 2, 0)]
    [TestCase(30, 1, 29)]
    [TestCase(30, 0, 30)]
    public void Query_WithOffset(int rowsCount, int offset, int expectedResult)
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rowsCount);

        var helper = QueryFactory.GetFactoryForConnection(conn);


        var cmd = helper.Select()
            .SetTable(tableName)
            .SetOffset(offset)
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(expectedResult));
    }


    [Test]
    [TestCase(3, 2, 1, 2)]
    [TestCase(3, 1, 1, 1)]
    [TestCase(1, 5, 5, 0)]
    [TestCase(30, 0, 0, 30)]
    [TestCase(30, 25, 0, 25)]
    [TestCase(30, 0, 25, 5)]
    [TestCase(30, 15, 25, 5)]
    [TestCase(30, 25, 4, 25)]
    [TestCase(30, 25, 5, 25)]
    [TestCase(30, 25, 6, 24)]
    [TestCase(30, 15, 30, 0)]
    [TestCase(30, 31, 0, 30)]
    public void Query_WithOffsetAndLimit(int rowsCount, int limit, int offset, int expectedResult)
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rowsCount);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .SetOffset(offset)
            .SetLimit(limit)
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(expectedResult));
    }

    [Test]
    public void Query_InnerJoin()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, 3);
        var tableName2 = SetupNewTable(conn, 10);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .Join(tableName2, JoinType.Inner, tableName, "col1", tableName2, "col1")
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void Query_RightJoin()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, 3);
        var tableName2 = SetupNewTable(conn, 10);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .Join(tableName2, JoinType.Right, tableName, "col1", tableName2, "col1")
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(10));
    }


    [Test]
    public void Query_LeftJoin()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, 3, 8);
        var tableName2 = SetupNewTable(conn, 10);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .Join(tableName2, JoinType.Left, tableName, "col1", tableName2, "col1")
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void Query_FullOuterJoin()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, 5);
        var tableName2 = SetupNewTable(conn, 5, 2);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .Join(tableName2, JoinType.FullOuter, tableName, "col1", tableName2, "col1")
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(7));
    }

    [Test]
    public void Query_OuterJoin_Manually()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, 5);
        var tableName2 = SetupNewTable(conn, 5, 2);

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var cmd = helper.Select()
            .SetTable(tableName)
            .Join(tableName2, JoinType.FullOuter, tableName, "col1", tableName2, "col1")
            .StartGroup()
            .And("col1", QueryVal.Null, ValueOperator.NotEquals, table: tableName)
            .StartGroup()
            .And("col1", QueryVal.Null, ValueOperator.NotEquals, table: tableName2)
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void Query_EmptyTable_ExpectZeroRows()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rows: 0); // No rows inserted

        var helper = QueryFactory.GetFactoryForConnection(conn);
        var cmd = helper.Select().SetTable(tableName).ToCommand();
        cmd.Connection = conn;

        using var reader = cmd.ExecuteReader();
        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(0));
    }
    [Test]
    public void Query_OrderByCol3_Ascending()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn, rows: 5, numberOffset: 10); // col3 = 11,12,13,14,15

        var helper = QueryFactory.GetFactoryForConnection(conn);
        var cmd = helper.Select()
            .SetTable(tableName)
            .AddOrder("col3", true)
            .ToCommand();

        cmd.Connection = conn;
        using var reader = cmd.ExecuteReader();

        int? last = null;
        while (reader.Read())
        {
            var current = reader.GetInt32(reader.GetOrdinal("col3"));
            if (last != null)
                Assert.That(current, Is.GreaterThan(last));
            last = current;
        }
    }
    [Test]
    public void Query_ColumnAlias_ExpectAliasedName()
    {
        using var conn = OpenNewConnection();
        var tableName = SetupNewTable(conn);

        var helper = QueryFactory.GetFactoryForConnection(conn);
        var cmd = helper.Select()
            .SetTable(tableName)
            .SetColumns(("col1", "c1"), ("col3", "c3"))
            .ToCommand();

        cmd.Connection = conn;
        var dt = new DataTable();
        dt.Load(cmd.ExecuteReader());

        Assert.That(dt.Columns, Has.Count.EqualTo(2));
        Assert.That(dt.Columns[0].ColumnName, Is.EqualTo("c1"));
        Assert.That(dt.Columns[1].ColumnName, Is.EqualTo("c3"));
    }



    [Test]
    public void Query_FilterNull_ExpectMatches()
    {
        using var conn = OpenNewConnection();
        var tableName = GenTableName();
        var quotedTable = _currentDBType switch
        {
            QueryFactory.SupportedDBTypes.Postgres => $"\"{tableName}\"",
            QueryFactory.SupportedDBTypes.SqlServer => $"[{tableName}]",
            QueryFactory.SupportedDBTypes.SQLite => $"\"{tableName}\"",
            _ => throw new ArgumentException()
        };

        var cmd = conn.CreateCommand();
        cmd.CommandText = $"Create Table {quotedTable} (col1 TEXT, col2 TEXT, col3 INTEGER)";
        cmd.ExecuteNonQuery();

        cmd = conn.CreateCommand();
        cmd.CommandText = $"Insert into {quotedTable} Values (NULL, 'val2', 123), ('notnull', NULL, 456)";
        cmd.ExecuteNonQuery();

        var helper = QueryFactory.GetFactoryForConnection(conn);

        var selectCmd = helper.Select()
            .SetTable(tableName)
            .AddFilter("col1", QueryVal.Null)
            .ToCommand();

        selectCmd.Connection = conn;
        using var reader = selectCmd.ExecuteReader();

        var count = 0;
        while (reader.Read())
            count++;

        Assert.That(count, Is.EqualTo(1));
    }

}