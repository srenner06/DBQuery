using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class SelectQueryBuilder(QueryCompiler compiler) : ConditionalQueryBuilder<SelectQueryBuilder>(compiler), ICalculatedColumn<SelectQueryBuilder>, IGroup<SelectQueryBuilder>, IOrder<SelectQueryBuilder>,
    ILimit<SelectQueryBuilder>, IOffset<SelectQueryBuilder>, IJoin<SelectQueryBuilder>
{
    protected HashSet<string> _calculatedColumns = new(StringComparer.InvariantCultureIgnoreCase);
    protected HashSet<string> _columns = new(StringComparer.InvariantCultureIgnoreCase);
    protected HashSet<string> _groups = new(StringComparer.InvariantCultureIgnoreCase);
    protected ulong? _limit;
    protected Dictionary<string, bool> _order = new(StringComparer.InvariantCultureIgnoreCase);
    protected ulong? _skip;


    public string[] GetColumns() => [.. _columns];
    public string[] GetCalculatedColumns() => [.. _calculatedColumns];
    protected List<Join> _joins = [];

    public string[] GetGroups() => [.. _groups];
    public ulong? GetLimit() => _limit;
    public ulong? GetOffset() => _skip;
    public Dictionary<string, bool> GetOrders() => new(_order);
    public Join[] GetJoins() => [.. _joins];


    public SelectQueryBuilder Join(string tableToJoin, JoinType type, string leftTable, string leftValue, string rightTable, string rightValue)
    {
        var join = new Join(tableToJoin, type, leftTable, leftValue, rightTable, rightValue);
        _joins.Add(join);
        return this;
    }


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
