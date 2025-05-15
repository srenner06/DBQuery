using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class DeleteQueryBuilder(QueryCompiler compiler) : ConditionalQueryBuilder<DeleteQueryBuilder>(compiler), IJoin<DeleteQueryBuilder>
{
    protected List<Join> _joins = [];

    public Join[] GetJoins() => [.. _joins];

    public DeleteQueryBuilder Join(string tableToJoin, JoinType type, string leftTable, string leftValue, string rightTable, string rightValue)
    {
        var join = new Join(tableToJoin, type, leftTable, leftValue, rightTable, rightValue);
        _joins.Add(join);
        return this;
    }
}