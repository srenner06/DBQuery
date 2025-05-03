using System.Data.Common;
using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class UpdateQueryBuilder(QueryCompiler compiler) : BaseQueryBuilder<UpdateQueryBuilder>(compiler), IUpdate, IWhere
{
    protected FilterGroup _filterGroups = new();
    protected Dictionary<string, QueryVal> _updates = new(StringComparer.InvariantCultureIgnoreCase);

    private bool _addToCurrentGroup;

    public Dictionary<string, QueryVal> GetUpdates() => new(_updates);
    IUpdate IUpdate.AddUpdate(string col, QueryVal val) => AddUpdate(col, val);

    IUpdate IUpdate.AddUpdateAsParameter<ParamType>(string col, string val, string paramname, out ParamType param)
        => AddUpdateAsParameter(col, val, paramname, out param);

    IUpdate IUpdate.SetUpdates(params (string col, QueryVal val)[] vals) => SetUpdates(vals);

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

    IWhere IWhere.StartGroup(LogicalOperation logicalOperation) => StartGroup(logicalOperation);
    IWhere IWhere.EndGroup() => EndGroup();

    public UpdateQueryBuilder ResetFilter()
    {
        _filterGroups = new FilterGroup();
        _addToCurrentGroup = false;
        return this;
    }

    public UpdateQueryBuilder And(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.And, op);

    public UpdateQueryBuilder Or(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.Or, op);

    public UpdateQueryBuilder AddFilter(string col, QueryVal val,
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
            _filterGroups.Next = (logicalOperation, newgroup);
        }

        return this;
    }

    public UpdateQueryBuilder AddFilterAsParameter<T>(string col, string val, string paramName, out T param,
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

    public UpdateQueryBuilder StartGroup(LogicalOperation logicalOperation = LogicalOperation.And)
    {
        _filterGroups.GetLast().Next = (logicalOperation, new FilterGroup());
        _addToCurrentGroup = true;
        return this;
    }

    public UpdateQueryBuilder EndGroup()
    {
        _addToCurrentGroup = false;
        return this;
    }

    public UpdateQueryBuilder AddUpdate(string col, QueryVal val)
    {
        if (!string.IsNullOrWhiteSpace(col))
            _updates[col.Trim()] = val;

        return this;
    }

    public UpdateQueryBuilder AddUpdateAsParameter<T>(string col, string val, string paramname, out T param)
        where T : DbParameter, new()
    {
        param = new T();
        if (string.IsNullOrWhiteSpace(col))
            return this;

        param.ParameterName = paramname;
        param.Value = val;

        var queryval = QueryVal.Param(param);
        _updates[col.Trim()] = queryval;

        return this;
    }

    public UpdateQueryBuilder SetUpdates(params (string col, QueryVal val)[] vals)
    {
        _updates.Clear();
        foreach (var val in vals)
            AddUpdate(val.col, val.val);
        return this;
    }
}
