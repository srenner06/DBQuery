using System.Data.Common;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;

namespace DBQuery.Compilers;

public class SQLiteCompiler(DbProviderFactory providerFactory) : QueryCompiler(providerFactory)
{
    protected override string HandleTableName(string table)
    {
        return QuoteIdentifier(table);
    }

    protected override string HandleColumn(string column)
    {
        return QuoteIdentifier(column);
    }

    protected override string HandleBool(BoolVal boolVal)
    {
        return boolVal.BoolValue
            ? "1"
            : "0";
    }
    protected override string HandleDate(DateVal dateVal)
    {
        return $"'{dateVal.DateValue:yyyy-MM-dd}'";
    }
    protected override string HandleDateTime(DateTimeVal dateVal)
    {
        return $"'{dateVal.DateTimeValue:d:yyyy-MM-dd HH:mm:ss}'";
    }

    protected override string HandleBlob(BlobVal blobVal)
    {
        var hex = BitConverter.ToString(blobVal.Data).Replace("-", "");
        return $"X'{hex}'";
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
            JoinType.Left => "Left",
            JoinType.Right => "Right",         // Supported in SQLite 3.39+
            JoinType.Inner => "Inner",
            JoinType.FullOuter => "Full Outer",// Supported in SQLite 3.39+
            JoinType.Full => "Full",           // Alias for Full Outer
            JoinType.Cross => "Cross",
            _ => throw new ArgumentException($"{type} is not Supported by SQLite")
        };
    }
    private string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null or whitespace.");

        var escaped = identifier.Replace("\"", "\"\""); // Escape embedded double quotes
        return $"\"{escaped}\"";
    }
}