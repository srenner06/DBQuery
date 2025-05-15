using System.Data.Common;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;

namespace DBQuery.Compilers;

public class PostgresCompiler(DbProviderFactory providerFactory) : QueryCompiler(providerFactory)
{
    protected override string HandleTableName(string table)
    {
        return QuoteIdentifier(table);
    }

    protected override string HandleColumn(string column)
    {
        return QuoteIdentifier(column);
    }

    protected override string HandleDateTime(DateTimeVal dateVal)
    {
        return $"'{dateVal.DateTimeValue:yyyy-MM-dd HH:mm:ss}'";
    }

    protected override string HandleDate(DateVal dateVal)
    {
        return $"'{dateVal.DateValue:yyyy-MM-dd}'";
    }

    protected override string HandleBool(BoolVal boolVal)
    {
        return boolVal.BoolValue ? "TRUE" : "FALSE";
    }

    protected override string HandleBlob(BlobVal blobVal)
    {
        var hex = BitConverter.ToString(blobVal.Data)
            .Replace("-", "");

        return $"E'\\\\x{hex}'";
    }

    protected override string HandleString(StringVal stringVal)
    {
        var escaped = stringVal.StringValue.Replace("'", "''"); // Escape single quotes
        return $"'{escaped}'";
    }

    protected override string GetNameForJoinType(JoinType type)
    {
        return type switch
        {
            JoinType.Left => "LEFT",
            JoinType.Right => "RIGHT",
            JoinType.Inner => "INNER",
            JoinType.FullOuter => "FULL OUTER",
            JoinType.Full => "FULL",
            JoinType.Cross => "CROSS",
            _ => throw new ArgumentException($"{type} is not supported by PostgreSQL")
        };
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or whitespace.");

        var escaped = identifier.Replace("\"", "\"\""); // Escape embedded quotes
        return $"\"{escaped}\"";
    }
}