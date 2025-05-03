using System.Data.Common;
using DBQuery.Compilers;
using DBQuery.QueryBuilders;

namespace DBQuery;

public class QueryFactory(QueryCompiler compiler)
{
    public SelectQueryBuilder Select(string table = "") => new SelectQueryBuilder(compiler).SetTable(table);
    public InsertQueryBuilder Insert(string table = "") => new InsertQueryBuilder(compiler).SetTable(table);
    public UpdateQueryBuilder Update(string table = "") => new UpdateQueryBuilder(compiler).SetTable(table);
    public DeleteQueryBuilder Delete(string table = "") => new DeleteQueryBuilder(compiler).SetTable(table);

    public enum SupportedDBTypes
    {
        SqlServer,
        SQLite
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
        else
            throw new NotSupportedException();

        return new QueryFactory(compiler);
    }
}