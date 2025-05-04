using System.Data.Common;
using System.Text;
using DBQuery.QueryBuilders;
using DBQuery.QueryVals;

namespace DBQuery.Compilers;

/// <summary>
///     Das ist nur eine einfach Hilfsklasse, um die Querys zu erstellen.
///     Es werden momentan nur einfache Filter unterstützt.
///     Für eine komplexere Anwedung bitte eine andere Klasse verwenden. z.B. SqlKata
/// </summary>
public abstract class QueryCompiler(DbProviderFactory providerFactory)
{
    protected delegate string CustomStringValueHandler(StringVal val);

    protected abstract string HandleTableName(string table);
    protected abstract string HandleColumnName(string column);
    protected virtual string HandleCalculatedValue(string value)
    {
        if (value.StartsWith('(') && value.EndsWith(')'))
            return value;
        else
            return $"({value})";
    }
    protected virtual string HandleNullOperator(ValueOperator valueOperator)
    {
        if (valueOperator == ValueOperator.Equals)
            return $"Is";
        if (valueOperator == ValueOperator.NotEquals)
            return $"Is not";

        throw new InvalidOperationException($"Operation {valueOperator} is not Valid for Null");
    }
    protected virtual string HandleComparison(ValueOperator valueOperator)
    {
        return valueOperator switch
        {
            ValueOperator.Equals => "=",
            ValueOperator.NotEquals => "!=",
            ValueOperator.Greater => ">",
            ValueOperator.Smaller => "<",
            ValueOperator.GreaterEquals => ">=",
            ValueOperator.SmallerEquals => "<=",
            ValueOperator.Like => "Like",
            _ => throw new InvalidOperationException($"Value Operator {valueOperator} is not Valid")
        };
    }
    protected virtual string HandleLogicalOperator(LogicalOperator logicalOperator)
    {
        return logicalOperator switch
        {
            LogicalOperator.And => "And",
            LogicalOperator.Or => "Or",
            _ => throw new InvalidOperationException($"Logical Operator {logicalOperator} is not Valid")
        };
    }
    protected virtual string HandleNull() => "NULL";
    protected virtual string HandleDefault() => "DEFAULT";
    protected abstract string HandleDateTime(DateTimeVal dateVal);
    protected abstract string HandleDate(DateVal dateVal);
    protected abstract string HandleBool(BoolVal boolVal);
    protected abstract string HandleString(StringVal stringVal);
    protected virtual string HandleQueryVal(QueryVal queryVal, CustomStringValueHandler? stringValueHandler)
    {
        if (queryVal is NullVal)
            return HandleNull();

        if (queryVal is DefaultVal)
            return HandleDefault();

        if (queryVal is ComputedVal computedVal)
            return HandleCalculatedValue(computedVal.ComputedValue);

        if (queryVal is DateTimeVal dateTimeVal)
            return HandleDateTime(dateTimeVal);

        if (queryVal is DateVal dateVal)
            return HandleDate(dateVal);

        if (queryVal is BoolVal boolVal)
            return HandleBool(boolVal);

        if (queryVal.GetType().IsGenericType && queryVal.GetType().GetGenericTypeDefinition() == typeof(NumericVal<>))
            return queryVal.Value!.ToString()!;

        if (queryVal is ParamVal paramVal)
            return paramVal.ParameterName;

        if (queryVal is StringVal stringVal)
        {
            if (stringValueHandler != null)
                return stringValueHandler(stringVal);
            else
                return HandleString(stringVal);
        }

        throw new Exception($"This Type of {nameof(QueryVal)} is not supported");
    }
    protected abstract string GetNameForJoinType(JoinType type);
    protected virtual string HandleOperation(string? table, string column, ValueOperator comparison, QueryVal value, CustomStringValueHandler? stringValueHandler)
    {
        var col = HandleColumnName(column);
        if (!string.IsNullOrWhiteSpace(table))
            col = $"{HandleTableName(table)}.{col}";

        return comparison switch
        {
            ValueOperator.In or ValueOperator.NotIn => HandleInOperation(col, comparison, (InListVal)value, stringValueHandler),
            ValueOperator.Between => HandleBetweenOperation(col, (BetweenVal)value, stringValueHandler),
            _ => HandleDefaultComparison(col, comparison, value, stringValueHandler)
        };
    }
    protected virtual string HandleInOperation(string col, ValueOperator op, InListVal val, CustomStringValueHandler? handler)
    {
        var items = string.Join(", ", val.Values.Select(v => HandleQueryVal(v, handler)));
        var not =
            op == ValueOperator.NotIn
            ? "NOT "
            : "";
        return $"{col} {not}IN ({items})";
    }

    protected virtual string HandleBetweenOperation(string col, BetweenVal val, CustomStringValueHandler? handler)
    {
        var from = HandleQueryVal(val.From, handler);
        var to = HandleQueryVal(val.To, handler);
        return $"{col} BETWEEN {from} AND {to}";
    }

    protected virtual string HandleDefaultComparison(string col, ValueOperator op, QueryVal val, CustomStringValueHandler? handler)
    {
        var opStr = val is NullVal
            ? HandleNullOperator(op)
            : HandleComparison(op);

        var valueStr = HandleQueryVal(val, handler);
        return $"{col} {opStr} {valueStr}";
    }

    protected virtual void AppendTable<T>(StringBuilder sb, T query) where T : ITable<T>
    {
        var table = query.GetTable();
        table = HandleTableName(table);
        sb.Append($" From {table}");
    }
    protected virtual void AppendAllColumns<T>(StringBuilder sb, T query) where T : ICalculatedColumn<T>
    {
        sb.Append(' ');

        var cols = query.GetColumns() ?? [];
        var calculatedfCols = query.GetCalculatedColumns() ?? [];

        if (cols.Length == 0 && calculatedfCols.Length == 0)
            sb.Append('*');
        else
        {
            if (cols.Length > 0)
            {
                sb.AppendJoin(", ", cols.Select(HandleColumnName));
                if (calculatedfCols.Length > 0)
                    sb.Append(", ");
            }
            if (calculatedfCols.Length > 0)
            {
                sb.AppendJoin(", ", calculatedfCols.Select(HandleCalculatedValue));
            }
        }
    }
    protected virtual void AppendColumns<T>(StringBuilder sb, T query) where T : IColumn<T>
    {
        sb.Append(' ');

        var cols = query.GetColumns() ?? [];

        if (cols.Length == 0)
            sb.Append('*');
        else
        {
            if (cols.Length > 0)
                sb.AppendJoin(", ", cols.Select(HandleColumnName));
        }
    }
    protected virtual void AppendInsertColumns<T>(StringBuilder sb, T query) where T : IColumn<T>
    {
        if (query.GetColumns().Length == 0)
            return;

        sb.Append(" (");
        sb.AppendJoin(", ", query.GetColumns().Select(HandleColumnName));
        sb.Append(')');
    }

    protected virtual void AppendWhere<T>(StringBuilder sb, T query, CustomStringValueHandler? stringValueHandler) where T : IWhere<T>
    {
        var condition = query.GetFilters();
        if (condition is null)
            return;

        sb.Append(" WHERE ");
        AppendCondition(sb, condition, stringValueHandler, isRoot: true);
    }

    protected virtual void AppendCondition(StringBuilder sb, ICondition condition, CustomStringValueHandler? stringValueHandler, bool isRoot = false)
    {
        switch (condition)
        {
            case FilterCondition fc:
                sb.Append(HandleOperation(fc.Table, fc.Column, fc.Operator, fc.Value, stringValueHandler));
                break;

            case ConditionGroup group:
                var needsParens = !isRoot && group.Conditions.Count > 1;

                if (needsParens)
                    sb.Append('(');

                for (var i = 0; i < group.Conditions.Count; i++)
                {
                    AppendCondition(sb, group.Conditions[i], stringValueHandler);
                    if (i < group.Conditions.Count - 1)
                    {
                        sb.Append(' ');
                        sb.Append(HandleLogicalOperator(group.Operator));
                        sb.Append(' ');
                    }
                }

                if (needsParens)
                    sb.Append(')');
                break;

            default:
                throw new InvalidOperationException("Unknown condition type.");
        }
    }

    protected virtual bool AppendGroupBy<T>(StringBuilder sb, T query) where T : IGroup<T>
    {
        if (query.GetGroups().Length == 0)
            return false;

        sb.Append(" GROUP BY ");
        sb.AppendJoin(", ", query.GetGroups().Select(HandleColumnName));
        return true;
    }

    protected virtual bool AppendOrderBy<T>(StringBuilder sb, T query) where T : IOrder<T>
    {
        if (query.GetOrders().Count == 0)
            return false;

        sb.Append(" ORDER BY ");
        var orders = query.GetOrders().Select(val => $"{HandleColumnName(val.Key)} {(val.Value ? "ASC" : "DESC")}");
        sb.AppendJoin(", ", orders);
        return true;
    }

    protected virtual bool AppendLimitAndOffset<T>(StringBuilder sb, T query) where T : ILimit<T>, IOffset<T>
    {
        var limit = query.GetLimit();
        var offset = query.GetOffset();
        if (limit is null && offset is null)
            return false;

        limit ??= long.MaxValue - 1;
        offset ??= 0;
        sb.AppendFormat(" LIMIT {0} OFFSET {1}", limit, offset);
        return true;
    }

    protected virtual void AppendUpdate<T>(StringBuilder sb, T query, CustomStringValueHandler? stringValueHandler) where T : IUpdate<T>
    {
        sb.Append(" SET ");
        var updates = query.GetUpdates().Select(kp =>
        {
            var val = HandleQueryVal(kp.Value, stringValueHandler);
            return $"{HandleColumnName(kp.Key)} = {val}";
        });
        sb.AppendJoin(", ", updates);
    }
    protected virtual void AppendInsertValues<T>(StringBuilder sb, T query, CustomStringValueHandler? stringValueHandler) where T : IInsert<T>
    {
        sb.Append(' ');

        var rows = query.GetRows();
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            if (rowIndex != 0)
                sb.Append(", ");

            var row = rows[rowIndex];
            sb.Append('(');

            foreach (var value in row)
            {
                var val = HandleQueryVal(value, stringValueHandler);
                sb.Append(val);
            }

            sb.Append(')');
        }
    }
    protected virtual void AppendJoins<T>(StringBuilder sb, T query) where T : IJoin<T>
    {
        var joins = query.GetJoins();
        if (joins.Length == 0)
            return;

        var joinTexts = joins
            .Select(j =>
            {
                var name = GetNameForJoinType(j.Type);

                var left = HandleColumnName(j.LeftColumn);
                if (string.IsNullOrWhiteSpace(j.LeftTable) == false)
                    left = $"{HandleTableName(j.LeftTable)}.{left}";

                var right = HandleColumnName(j.RightColumn);
                if (string.IsNullOrWhiteSpace(j.RightTable) == false)
                    right = $"{HandleTableName(j.RightTable)}.{right}";

                return $"{name} Join {HandleTableName(j.TableToJoin)} On {left} = {right}";
            });

        sb.Append(' ');
        sb.AppendJoin(" ", joinTexts);
    }


    public string ToQuery<T>(T query) where T : BaseQueryBuilder<T>
    {
        if (query is SelectQueryBuilder select)
            return CompileSelectQuery(select);

        if (query is UpdateQueryBuilder update)
            return CompileUpdateQuery(update);

        if (query is InsertQueryBuilder insert)
            return CompileInsertQuery(insert);

        if (query is DeleteQueryBuilder delete)
            return CompileDeleteQuery(delete);

        throw new NotImplementedException();
    }

    public DbCommand ToCommand<T>(T query) where T : BaseQueryBuilder<T>
    {
        if (query is SelectQueryBuilder select)
            return CompileSelectCommand(select);

        if (query is UpdateQueryBuilder update)
            return CompileUpdateCommand(update);

        if (query is InsertQueryBuilder insert)
            return CompileInsertCommand(insert);

        if (query is DeleteQueryBuilder delete)
            return CompileDeleteCommand(delete);

        throw new NotImplementedException();
    }


    protected virtual DbCommand BuildCommand<TQuery>(TQuery query, Func<TQuery, CustomStringValueHandler?, StringBuilder> func) where TQuery : BaseQueryBuilder<TQuery>
    {
        var parameters = new List<DbParameter>();
        string stringValueHandler(StringVal stringVal)
        {
            var name = $"@p{parameters.Count}";
            var param = providerFactory.CreateParameter()!;
            param.ParameterName = name;
            param.Value = stringVal.StringValue;
            parameters.Add(param);
            return name;
        }

        var sb = func(query, stringValueHandler);

        var command = providerFactory.CreateCommand()!;
        command.CommandText = sb.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        return command;
    }

    #region Select
    public virtual string CompileSelectQuery(SelectQueryBuilder select)
    {
        var sb = BuildSelectQuery(select, null);
        return sb.ToString().Trim();
    }
    public virtual DbCommand CompileSelectCommand(SelectQueryBuilder select)
    {
        return BuildCommand(select, BuildSelectQuery);
    }

    protected virtual StringBuilder BuildSelectQuery(SelectQueryBuilder select, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("SELECT");

        AppendAllColumns(sb, select);
        AppendTable(sb, select);
        AppendJoins(sb, select);
        AppendWhere(sb, select, stringValueHandler);
        AppendGroupBy(sb, select);
        AppendOrderBy(sb, select);
        AppendLimitAndOffset(sb, select);

        return sb;
    }
    #endregion

    #region Update
    public virtual string CompileUpdateQuery(UpdateQueryBuilder update)
    {
        var sb = BuildUpdateQuery(update, null);
        return sb.ToString().Trim();
    }
    public virtual DbCommand CompileUpdateCommand(UpdateQueryBuilder update)
    {
        return BuildCommand(update, BuildUpdateQuery);
    }
    protected virtual StringBuilder BuildUpdateQuery(UpdateQueryBuilder update, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("UPDATE");
        AppendTable(sb, update);
        AppendUpdate(sb, update, stringValueHandler);
        AppendWhere(sb, update, stringValueHandler);

        return sb;
    }
    #endregion

    #region Insert
    public virtual string CompileInsertQuery(InsertQueryBuilder insert)
    {
        var sb = BuildInsertQuery(insert, null);
        return sb.ToString().Trim();
    }
    public virtual DbCommand CompileInsertCommand(InsertQueryBuilder insert)
    {
        return BuildCommand(insert, BuildInsertQuery);
    }
    protected virtual StringBuilder BuildInsertQuery(InsertQueryBuilder insert, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("INSERT INTO");
        AppendTable(sb, insert);
        AppendInsertColumns(sb, insert);
        sb.Append(" VALUES");
        AppendInsertValues(sb, insert, stringValueHandler);

        return sb;
    }
    #endregion

    #region Delete
    public virtual string CompileDeleteQuery(DeleteQueryBuilder delete)
    {
        var sb = BuildDeleteQuery(delete, null);
        return sb.ToString().Trim();
    }
    public virtual DbCommand CompileDeleteCommand(DeleteQueryBuilder delete)
    {
        return BuildCommand(delete, BuildDeleteQuery);
    }
    protected virtual StringBuilder BuildDeleteQuery(DeleteQueryBuilder delete, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("DELETE FROM");
        AppendTable(sb, delete);
        AppendWhere(sb, delete, stringValueHandler);

        return sb;
    }
    #endregion
}
