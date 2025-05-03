using System.Data.Common;
using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class DeleteQueryBuilder(QueryCompiler compiler) : BaseQueryBuilder<DeleteQueryBuilder>(compiler), IWhere
{
    protected FilterGroup _filterGroups = new();

    private bool _addToCurrentGroup;

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

    public DeleteQueryBuilder ResetFilter()
    {
        _filterGroups = new FilterGroup();
        _addToCurrentGroup = false;
        return this;
    }

    public DeleteQueryBuilder And(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.And, op);

    public DeleteQueryBuilder Or(string col, QueryVal val, string op = "=")
        => AddFilter(col, val, LogicalOperation.Or, op);

    public DeleteQueryBuilder AddFilter(string col, QueryVal val,
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

    public DeleteQueryBuilder AddFilterAsParameter<T>(string col, string val, string paramName, out T param,
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

    public DeleteQueryBuilder StartGroup(LogicalOperation logicalOperation = LogicalOperation.And)
    {
        _filterGroups.GetLast().Next = (logicalOperation, new FilterGroup());
        _addToCurrentGroup = true;
        return this;
    }

    public DeleteQueryBuilder EndGroup()
    {
        _addToCurrentGroup = false;
        return this;
    }
}
