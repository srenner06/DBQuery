using System.Data.Common;
using DBQuery.Compilers;
using DBQuery.QueryVals;

namespace DBQuery.QueryBuilders;

public abstract class ConditionalQueryBuilder<T>(QueryCompiler compiler) : BaseQueryBuilder<T>(compiler), IWhere<T>
    where T : ConditionalQueryBuilder<T>
{
    protected readonly Stack<ConditionGroup> ConditionStack = [];
    protected ConditionGroup? _rootConditionGroup;

    public ConditionGroup? GetFilters() => _rootConditionGroup;

    public T ResetFilter()
    {
        ConditionStack.Clear();
        _rootConditionGroup = null;
        return (T)this;
    }

    public T And(string col, QueryVal val, ValueOperator valueOperator = ValueOperator.Equals, string? table = null)
        => AddFilter(col, val, LogicalOperator.And, valueOperator, table);

    public T Or(string col, QueryVal val, ValueOperator valueOperator = ValueOperator.Equals, string? table = null)
        => AddFilter(col, val, LogicalOperator.Or, valueOperator, table);

    public T AddFilter(string col, QueryVal val, LogicalOperator logicalOperator = LogicalOperator.And,
        ValueOperator valueOperator = ValueOperator.Equals, string? table = null)
    {
        var filter = new FilterCondition(col, val, valueOperator, logicalOperator, table);

        if (ConditionStack.TryPeek(out var group))
        {
            group.Conditions.Add(filter);
        }
        else if (_rootConditionGroup is not null)
        {
            var newGroup = new ConditionGroup(logicalOperator);
            newGroup.Conditions.Add(_rootConditionGroup);
            newGroup.Conditions.Add(filter);
            _rootConditionGroup = newGroup;
        }
        else
        {
            _rootConditionGroup = new ConditionGroup(logicalOperator);
            _rootConditionGroup.Conditions.Add(filter);
        }

        return (T)this;
    }

    public T StartGroup(LogicalOperator logicalOperator = LogicalOperator.And)
    {
        var group = new ConditionGroup(logicalOperator);

        if (ConditionStack.TryPeek(out var parent))
            parent.Conditions.Add(group);
        else if (_rootConditionGroup is not null)
        {
            var wrapper = new ConditionGroup(logicalOperator);
            wrapper.Conditions.Add(_rootConditionGroup);
            wrapper.Conditions.Add(group);
            _rootConditionGroup = wrapper;
        }
        else
        {
            _rootConditionGroup = group;
        }

        ConditionStack.Push(group);
        return (T)this;
    }

    public T EndGroup()
    {
        if (ConditionStack.Count > 0)
            ConditionStack.Pop();

        return (T)this;
    }

    public T AddFilterAsParameter<TParam>(string col, string val, string paramName, out TParam param,
        LogicalOperator logicalOperation = LogicalOperator.And, ValueOperator valueOperator = ValueOperator.Equals) where TParam : DbParameter, new()
    {
        param = new TParam();
        if (string.IsNullOrWhiteSpace(col))
            return (T)this;

        param.ParameterName = paramName;
        param.Value = val;

        AddFilter(col, param, logicalOperation, valueOperator);

        return (T)this;
    }
}