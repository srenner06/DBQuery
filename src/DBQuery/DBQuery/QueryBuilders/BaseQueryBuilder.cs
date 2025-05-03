using System.Data.Common;
using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public abstract class BaseQueryBuilder<T>(QueryCompiler compiler) : ITable where T : BaseQueryBuilder<T>
{
    protected string _tableName = "";

    ITable ITable.SetTable(string name) => SetTable(name);

    public string GetTable() => _tableName;

    public virtual T SetTable(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _tableName = name.Trim();
        return (T)this;
    }

    public string ToQuery() => compiler.ToQuery((T)this);

    /// <summary>
    /// Gibs das Query als String zurück. <br></br>
    /// Wenn externe strings (wie User Input) verwendet werden ist SQL-Injection möglich.<br></br>
    /// Deshalb sollte in diesen Fällen <see cref="ToCommand"/> verwendet werden
    /// </summary>
    /// <returns></returns>
    public DbCommand ToCommand() => compiler.ToCommand((T)this);
}


public enum LogicalOperation
{
    And,
    Or
}

public class Filter(string col, QueryVal val, string op = "=")
{
    public string Col = col.Trim();
    public (LogicalOperation op, Filter)? Next;
    public string Operation = op.Trim();
    public QueryVal Val = val;

    public Filter GetLast()
    {
        var last = this;
        while (last.Next.HasValue)
            last = last.Next.Value.Item2;
        return last;
    }
}

public class FilterGroup
{
    public Filter? Filters;
    public (LogicalOperation op, FilterGroup)? Next;

    public FilterGroup GetLast()
    {
        var last = this;
        while (last.Next.HasValue)
            last = last.Next.Value.Item2;
        return last;
    }
}
