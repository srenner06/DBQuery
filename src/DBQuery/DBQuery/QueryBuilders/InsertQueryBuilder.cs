using DBQuery.Compilers;

namespace DBQuery.QueryBuilders;

public class InsertQueryBuilder(QueryCompiler compiler) : BaseQueryBuilder<InsertQueryBuilder>(compiler), IInsert, IColumn
{
    protected HashSet<string> _columns = new(StringComparer.InvariantCultureIgnoreCase);
    protected List<QueryVal[]> _values = [];

    public string[] GetColumns() => _columns.ToArray();

    IColumn IColumn.SetColumns(params string[] columns) => SetColumns(columns);
    IColumn IColumn.AddColumns(params string[] columns) => AddColumns(columns);
    public QueryVal[][] GetRows() => _values.ToArray();
    public bool HasRows => _values.Count > 0;
    IInsert IInsert.SetRows(params QueryVal[][] rows) => SetRows(rows);
    IInsert IInsert.AddRows(params QueryVal[][] rows) => AddRows(rows);
    IInsert IInsert.AddRow(params QueryVal[] row) => AddRow(row);

    public InsertQueryBuilder SetColumns(params string[] columns)
    {
        _columns.Clear();
        AddColumns(columns);
        return this;
    }

    public InsertQueryBuilder AddColumns(params string[] columns)
    {
        foreach (var col in columns.Where(c => !string.IsNullOrWhiteSpace(c)))
            _columns.Add(col);
        return this;
    }

    public InsertQueryBuilder SetRows(params QueryVal[][] rows)
    {
        _values.Clear();
        _values.AddRange(rows);
        return this;
    }

    public InsertQueryBuilder AddRows(params QueryVal[][] rows)
    {
        _values.AddRange(rows);
        return this;
    }

    public InsertQueryBuilder AddRow(params QueryVal[] row)
    {
        _values.Add(row);
        return this;
    }
}