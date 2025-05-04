using System.Data.Common;
using System.Text;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;

namespace DBQuery.Compilers;

public class SqlServerCompiler(DbProviderFactory providerFactory) : QueryCompiler(providerFactory)
{
    protected override StringBuilder BuildSelectQuery(SelectQueryBuilder select, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("SELECT");

        var limit = select.GetLimit();
        var offset = select.GetOffset();

        if (limit != null && offset == null)
            sb.Append($" TOP {limit}");

        AppendAllColumns(sb, select);
        AppendTable(sb, select);
        AppendJoins(sb, select);
        AppendWhere(sb, select, stringValueHandler);

        AppendGroupBy(sb, select);
        AppendOrderBy_SqlServer(sb, select, offset != null);

        if (offset != null)
        {
            sb.Append($" Offset {offset} Rows");
            if (limit != null)
                sb.Append($" Fetch NEXT {limit} ROWS ONLY");
        }

        return sb;
    }

    protected virtual bool AppendOrderBy_SqlServer<T>(StringBuilder sb, T query, bool hasOffset) where T : IOrder<T>
    {
        if (AppendOrderBy(sb, query) || hasOffset == false)
            return true;

        // Damit Offset und Fetch funktionieren muss ein Order by vorhanden sein
        sb.Append(" Order By (Select 0)");
        return true;
    }
    protected override string GetNameForJoinType(JoinType type)
    {
        return type switch
        {
            JoinType.Left => "Left",
            JoinType.Right => "Right",
            JoinType.Inner => "Inner",
            JoinType.FullOuter => "Full Outer",
            JoinType.Full => "Full",
            JoinType.Cross => "Cross",
            _ => throw new ArgumentException($"{type} is not Supported by SQL-Server")
        };
    }

    protected override string HandleTableName(string table)
    {
        if (table.StartsWith('[') && table.EndsWith(']'))
            return table;

        return $"[{table}]";
    }
    protected override string HandleColumnName(string column)
    {
        if (column.StartsWith('[') && column.EndsWith(']'))
            return column;

        return $"[{column}]";
    }
    protected override string HandleDateTime(DateTimeVal dateVal)
    {
        var d = dateVal.DateTimeValue;
        return $"'{d:yyyy-MM-dd HH:mm:ss}'";
    }
    protected override string HandleDate(DateVal dateVal)
    {
        var d = dateVal.DateValue;
        return $"'{d:yyyy-MM-dd}'";
    }
    protected override string HandleBool(BoolVal boolVal)
    {
        return boolVal.BoolValue
            ? "1"
            : "0";
    }
    protected override string HandleString(StringVal stringVal)
    {
        return $"'{stringVal.StringValue}'";
    }
}