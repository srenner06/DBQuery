using System.Data.Common;
using DBQuery.QueryBuilders;

namespace DBQuery;

public interface ITable
{
    public ITable SetTable(string name);
    internal string GetTable();
    //TODO sr: joins
}

public interface IColumn
{
    public IColumn SetColumns(params string[] columns);
    public IColumn AddColumns(params string[] columns);
    public string[] GetColumns();
}

public interface ICalculatedColumn : IColumn
{
    public ICalculatedColumn SetCalculatedColumns(params string[] columns);
    public ICalculatedColumn AddCalculatedColumns(params string[] columns);
    internal string[] GetCalculatedColumns();
}

public interface IWhere
{
    public IWhere ResetFilter();
    public IWhere StartGroup(LogicalOperation operation);
    public IWhere EndGroup();

    public IWhere AddFilterAsParameter<ParamType>(string col, string val, string paramname, out ParamType param,
        LogicalOperation logicalOperation = LogicalOperation.And, string op = "=") where ParamType : DbParameter, new();

    public IWhere AddFilter(string col, QueryVal val, LogicalOperation logicalOperation = LogicalOperation.And,
        string op = "=");

    internal FilterGroup GetFilters();
}

public interface IGroup
{
    public IGroup SetGroups(params string[] groups);
    public IGroup AddGroups(params string[] groups);
    internal string[] GetGroups();
}

public interface IOrder
{
    public IOrder AddOrder(string col, bool ascending);
    internal Dictionary<string, bool> GetOrders();
}

public interface ILimit
{
    public ILimit SetLimit(ulong? limit);
    public ulong? GetLimit();
}

public interface IOffset
{
    public IOffset SetOffset(ulong? offset);
    public ulong? GetOffset();
}

public interface IUpdate
{
    public IUpdate AddUpdate(string col, QueryVal val);

    public IUpdate AddUpdateAsParameter<ParamType>(string col, string val, string paramname, out ParamType param)
        where ParamType : DbParameter, new();

    public IUpdate SetUpdates(params (string col, QueryVal val)[] vals);
    public Dictionary<string, QueryVal> GetUpdates();
}

public interface IInsert
{
    public bool HasRows { get; }
    public IInsert SetRows(params QueryVal[][] rows);
    public IInsert AddRows(params QueryVal[][] rows);
    public IInsert AddRow(params QueryVal[] row);
    public QueryVal[][] GetRows();
}