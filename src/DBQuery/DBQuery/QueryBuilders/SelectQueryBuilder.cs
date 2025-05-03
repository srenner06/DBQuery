using System.Data.Common;
using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class SelectQueryBuilder(QueryCompiler compiler) : BaseQueryBuilder<SelectQueryBuilder>(compiler), ICalculatedColumn, IWhere, IGroup, IOrder,
    ILimit, IOffset
{
    protected HashSet<string> _calculatedColumns = new(StringComparer.InvariantCultureIgnoreCase);
    protected HashSet<string> _columns = new(StringComparer.InvariantCultureIgnoreCase);
    protected FilterGroup _filterGroups = new(); // Changed to List<object>
    protected HashSet<string> _groups = new(StringComparer.InvariantCultureIgnoreCase);
    protected ulong? _limit;
    protected Dictionary<string, bool> _order = new(StringComparer.InvariantCultureIgnoreCase);
    protected ulong? _skip;

    private bool _addToCurrentGroup;

    public string[] GetColumns() => _columns.ToArray();
    public string[] GetCalculatedColumns() => _calculatedColumns.ToArray();

    ICalculatedColumn ICalculatedColumn.SetCalculatedColumns(params string[] columns)
        => SetCalculatedColumns(columns);

    ICalculatedColumn ICalculatedColumn.AddCalculatedColumns(params string[] columns)
        => AddCalculatedColumns(columns);

    IColumn IColumn.SetColumns(params string[] columns) => SetColumns(columns);
    IColumn IColumn.AddColumns(params string[] columns) => AddColumns(columns);
    public string[] GetGroups() => _groups.ToArray();
    IGroup IGroup.SetGroups(params string[] groups) => SetGroups(groups);
    IGroup IGroup.AddGroups(params string[] groups) => AddGroups(groups);
    public ulong? GetLimit() => _limit;
    ILimit ILimit.SetLimit(ulong? limit) => SetLimit(limit);
    public ulong? GetOffset() => _skip;
    IOffset IOffset.SetOffset(ulong? offset) => SetOffset(offset);
    public Dictionary<string, bool> GetOrders() => new(_order);
    IOrder IOrder.AddOrder(string col, bool ascending) => AddOrder(col, ascending);

    public FilterGroup GetFilters()
    {
        var filter = _filterGroups;
        while (filter.Filters is null && filter.Next is not null)
            filter = filter.Next!.Value.Item2;
        return filter;
    }

    IWhere IWhere.ResetFilter() => ResetFilter();

    IWhere IWhere.AddFilterAsParameter<ParamType>(string col, string val, string paramname, out ParamType param,
        LogicalOperation logicalOperation, string op)
        => AddFilterAsParameter(col, val, paramname, out param, logicalOperation, op);

    IWhere IWhere.AddFilter(string col, QueryVal val, LogicalOperation logicalOperation, string op)
        => AddFilter(col, val, logicalOperation, op);

    IWhere IWhere.StartGroup(LogicalOperation operation) => StartGroup(operation);
    IWhere IWhere.EndGroup() => EndGroup();

    public SelectQueryBuilder SetLimit(ulong? limit)
    {
        _limit = limit > 0 ? limit : null;
        return this;
    }

    public SelectQueryBuilder SetOffset(ulong? offset)
    {
        _skip = offset > 0 ? offset : null;
        return this;
    }

    public SelectQueryBuilder SetLimit(long? limit)
    {
        _limit = limit > 0 ? (ulong)limit : null;
        return this;
    }

    public SelectQueryBuilder SetOffset(long? offset)
    {
        _skip = offset > 0 ? (ulong)offset : null;
        return this;
    }

    public SelectQueryBuilder SetColumns(params string[] columns)
    {
        _columns.Clear();
        AddColumns(columns);
        return this;
    }

    public SelectQueryBuilder AddColumns(params string[] columns)
    {
        foreach (var col in columns.Where(c => !string.IsNullOrWhiteSpace(c)))
            _columns.Add(col.Trim());
        return this;
    }

    public SelectQueryBuilder SetCalculatedColumns(params string[] columns)
    {
        _calculatedColumns.Clear();
        AddCalculatedColumns(columns);
        return this;
    }

    public SelectQueryBuilder AddCalculatedColumns(params string[] columns)
    {
        foreach (var col in columns.Where(c => !string.IsNullOrWhiteSpace(c)))
            _calculatedColumns.Add(col.Trim());
        return this;
    }

    public SelectQueryBuilder ResetFilter()
    {
        _filterGroups = new FilterGroup();
        _addToCurrentGroup = false;
        return this;
    }

    public SelectQueryBuilder And(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.And, op);

    public SelectQueryBuilder Or(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.Or, op);

    public SelectQueryBuilder AddFilter(string col, QueryVal val,
        LogicalOperation logicalOperation = LogicalOperation.And, string op = "=")
    {
        if (string.IsNullOrWhiteSpace(col))
            return this;

        var newFilter = new Filter(col, val, op);

        if (_addToCurrentGroup)
        {
            var lastGroup = _filterGroups.GetLast();
            if (lastGroup.Filters is null)
                lastGroup.Filters = newFilter;
            else
            {
                var last = lastGroup.Filters.GetLast();
                last.Next = (logicalOperation, newFilter);
            }
        }
        else
        {
            var newgroup = new FilterGroup
            {
                Filters = newFilter
            };
            _filterGroups.GetLast().Next = (logicalOperation, newgroup);
        }

        return this;
    }

    public SelectQueryBuilder AddFilterAsParameter<T>(string col, string val, string paramName, out T param,
        LogicalOperation logicalOperation = LogicalOperation.And, string op = "=") where T : DbParameter, new()
    {
        param = new T();
        if (string.IsNullOrWhiteSpace(col))
            return this;

        param.ParameterName = paramName;
        param.Value = val;

        AddFilter(col, param, logicalOperation, op);

        return this;
    }

    /// <summary>
    ///     Momentan kann nur eine Gruppe gleichzeitig bearbeitet werden.
    /// </summary>
    /// <param name="logicalOperation"></param>
    /// <returns></returns>
    public SelectQueryBuilder StartGroup(LogicalOperation logicalOperation = LogicalOperation.And)
    {
        _filterGroups.GetLast().Next = (logicalOperation, new FilterGroup());
        _addToCurrentGroup = true;
        return this;
    }

    public SelectQueryBuilder EndGroup()
    {
        _addToCurrentGroup = false;
        return this;
    }

    public SelectQueryBuilder SetGroups(params string[] groups)
    {
        _groups.Clear();
        AddGroups(groups);
        return this;
    }

    public SelectQueryBuilder AddGroups(params string[] groups)
    {
        foreach (var group in groups.Where(g => !string.IsNullOrWhiteSpace(g)))
            _groups.Add(group.Trim());
        return this;
    }

    public SelectQueryBuilder AddOrder(string col, bool ascending)
    {
        if (!string.IsNullOrWhiteSpace(col))
            _order[col.Trim()] = ascending;
        return this;
    }
}
