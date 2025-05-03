using System.Data.Common;
using DBQuery.QueryBuilders;

namespace DBQuery.Compilers;

/// <summary>
///     Das ist nur eine einfach Hilfsklasse, um die Querys zu erstellen.
///     Es werden momentan nur einfache Filter unterstützt.
///     Für eine komplexere Anwedung bitte eine andere Klasse verwenden. z.B. SqlKata
/// </summary>
public abstract class QueryCompiler
{
    protected delegate string CustomStringValueHandler(QueryVal.StringVal val);
    protected virtual string GetQueryString(QueryVal value, CustomStringValueHandler? stringValueHandler)
    {
        if (value is QueryVal.StringVal stringVal && stringValueHandler != null)
            return stringValueHandler(stringVal);
        else
            return value.GetString();
    }

    public string ToQuery<T>(T query) where T : BaseQueryBuilder<T>
    {
        if (query is SelectQueryBuilder select)
            return CompileSelectQuery(select);

        if (query is UpdateQueryBuilder update)
            return CompileUpdateQuery(update);

        if (query is InsertQueryBuilder insert)
            return CompileInsertQuery(insert);

        if (query is DeleteQueryBuilder delete)
            return CompileDeleteQuery(delete);

        throw new NotImplementedException();
    }

    public DbCommand ToCommand<T>(T query) where T : BaseQueryBuilder<T>
    {
        if (query is SelectQueryBuilder select)
            return CompileSelectCommand(select);

        if (query is UpdateQueryBuilder update)
            return CompileUpdateCommand(update);

        if (query is InsertQueryBuilder insert)
            return CompileInsertCommand(insert);

        if (query is DeleteQueryBuilder delete)
            return CompileDeleteCommand(delete);

        throw new NotImplementedException();
    }

    public abstract string CompileSelectQuery(SelectQueryBuilder select);
    public abstract string CompileUpdateQuery(UpdateQueryBuilder update);
    public abstract string CompileInsertQuery(InsertQueryBuilder insert);
    public abstract string CompileDeleteQuery(DeleteQueryBuilder delete);

    public abstract DbCommand CompileSelectCommand(SelectQueryBuilder select);
    public abstract DbCommand CompileUpdateCommand(UpdateQueryBuilder update);
    public abstract DbCommand CompileInsertCommand(InsertQueryBuilder insert);
    public abstract DbCommand CompileDeleteCommand(DeleteQueryBuilder delete);
}
