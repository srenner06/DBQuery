using System.Data.Common;
using DBQuery.Compilers;
using DBQuery.QueryBuilders;

namespace DBQuery;

public class QueryFactory
{
    private readonly QueryCompiler Compiler;

    public QueryFactory(QueryCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);
        Compiler = compiler;
    }

    public SelectQueryBuilder Select(string table = "") => new SelectQueryBuilder(Compiler).SetTable(table);
    public InsertQueryBuilder Insert(string table = "") => new InsertQueryBuilder(Compiler).SetTable(table);
    public UpdateQueryBuilder Update(string table = "") => new UpdateQueryBuilder(Compiler).SetTable(table);
    public DeleteQueryBuilder Delete(string table = "") => new DeleteQueryBuilder(Compiler).SetTable(table);

    public enum SupportedDBTypes
    {
        SqlServer,
        SQLite,
        Postgres
    }

    public static QueryFactory GetFactoryForConnection(DbConnection conn)
    {
        var factory = DbProviderFactories.GetFactory(conn)!;
        return GetQueryFactory(factory);
    }

    public static QueryFactory GetQueryFactory(DbProviderFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var name = factory.GetType().Namespace!.ToLower();

        QueryCompiler compiler;

        if (name.Contains(".sqlclient")) // do not mistake this for mysqlclient
            compiler = new SqlServerCompiler(factory);
        else if (name.Contains(".sqlite"))
            compiler = new SQLiteCompiler(factory);
        else if (name.Contains("npgsql"))
            compiler = new PostgresCompiler(factory);
        else
            throw new NotSupportedException();

        return new QueryFactory(compiler);
    }
}