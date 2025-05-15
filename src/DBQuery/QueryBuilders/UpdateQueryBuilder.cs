using System.Data.Common;
using DBQuery.Compilers;
using DBQuery.QueryVals;

namespace DBQuery.QueryBuilders;

public class UpdateQueryBuilder(QueryCompiler compiler) : ConditionalQueryBuilder<UpdateQueryBuilder>(compiler), IUpdate<UpdateQueryBuilder>, IJoin<UpdateQueryBuilder>
{
    protected Dictionary<string, QueryVal> _updates = new(StringComparer.InvariantCultureIgnoreCase);
    protected List<Join> _joins = [];

    public Join[] GetJoins() => [.. _joins];

    public UpdateQueryBuilder Join(string tableToJoin, JoinType type, string leftTable, string leftValue, string rightTable, string rightValue)
    {
        var join = new Join(tableToJoin, type, leftTable, leftValue, rightTable, rightValue);
        _joins.Add(join);
        return this;
    }

    public Dictionary<string, QueryVal> GetUpdates() => new(_updates);

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