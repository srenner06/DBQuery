using System.Data.Common;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;

namespace DBQuery;

public interface ITable<T> where T : ITable<T>
{
    public T SetTable(string name);
    internal string GetTable();
}

public interface IJoin<T> where T : IJoin<T>
{
    public T Join(string tableToJoin, JoinType type, string leftTable, string leftValue, string rightTable, string rightValue);
    internal Join[] GetJoins();
}

public interface IColumn<T> where T : IColumn<T>
{
    public T SetColumns(params string[] columns);
    public T AddColumns(params string[] columns);
    internal string[] GetColumns();
}
public interface IAliasColumn<T> : IColumn<T> where T : IAliasColumn<T>
{
    public T SetColumns(params (string col, string? alias)[] columns);
    public T AddColumns(params (string col, string? alias)[] columns);
    internal (string col, string? alias)[] GetAliasColumns();
}
public interface ICalculatedColumn<T> : IAliasColumn<T> where T : ICalculatedColumn<T>
{
    public T SetCalculatedColumns(params string[] columns);
    public T AddCalculatedColumns(params string[] columns);
    public T SetCalculatedColumns(params (string col, string? alias)[] columns);
    public T AddCalculatedColumns(params (string col, string? alias)[] columns);
    internal (string col, string? alias)[] GetCalculatedColumns();
}

public interface IWhere<T> where T : IWhere<T>
{
    public T ResetFilter();
    public T StartGroup(LogicalOperator operation);
    public T EndGroup();

    //TODO sr: Brauchts das noch mit ToCommand() ???
    public T AddFilterAsParameter<ParamType>(string col, string val, string paramname, out ParamType param,
        LogicalOperator logicalOperation = LogicalOperator.And, ValueOperator valueOperator = ValueOperator.Equals) where ParamType : DbParameter, new();

    public T AddFilter(string col, QueryVal val, LogicalOperator logicalOperation = LogicalOperator.And, ValueOperator valueOperator = ValueOperator.Equals, string? table = null);

    internal ConditionGroup? GetFilters();
}

public interface IGroup<T> where T : IGroup<T>
{
    public T SetGroups(params string[] groups);
    public T AddGroups(params string[] groups);
    internal string[] GetGroups();
}

public interface IOrder<T> where T : IOrder<T>
{
    public T AddOrder(string col, bool ascending);
    internal Dictionary<string, bool> GetOrders();
}

public interface ILimit<T> where T : ILimit<T>
{
    public T SetLimit(ulong? limit);
    internal ulong? GetLimit();
}

public interface IOffset<T> where T : IOffset<T>
{
    public T SetOffset(ulong? offset);
    internal ulong? GetOffset();
}

public interface IUpdate<T> where T : IUpdate<T>
{
    public T AddUpdate(string col, QueryVal val);

    public T AddUpdateAsParameter<ParamType>(string col, string val, string paramname, out ParamType param)
        where ParamType : DbParameter, new();

    public T SetUpdates(params (string col, QueryVal val)[] vals);
    internal Dictionary<string, QueryVal> GetUpdates();
}

public interface IInsert<T> where T : IInsert<T>
{
    public bool HasRows { get; }
    public T SetRows(params QueryVal[][] rows);
    public T AddRows(params QueryVal[][] rows);
    public T AddRow(params QueryVal[] row);
    internal QueryVal[][] GetRows();
}