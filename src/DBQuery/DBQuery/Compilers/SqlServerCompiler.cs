using System.Data.Common;
using System.Text;
using DBQuery.QueryBuilders;

namespace DBQuery.Compilers;

public class SqlServerCompiler(DbProviderFactory providerFactory) : QueryCompiler
{
    #region Select
    public override string CompileSelectQuery(SelectQueryBuilder select)
    {
        var sb = BuildSelectQuery(select, null);
        return sb.ToString().Trim();
    }

    public override DbCommand CompileSelectCommand(SelectQueryBuilder select)
    {
        var parameters = new List<DbParameter>();
        CustomStringValueHandler stringValueHandler = (QueryVal.StringVal stringVal) =>
        {
            var name = $"@p{parameters.Count}";
            var param = providerFactory.CreateParameter()!;
            param.ParameterName = name;
            param.Value = (string)stringVal.Value;
            parameters.Add(param);
            return name;
        };

        var sb = BuildSelectQuery(select, stringValueHandler);

        var command = providerFactory.CreateCommand()!;
        command.CommandText = sb.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        return command;
    }

    private StringBuilder BuildSelectQuery(SelectQueryBuilder select, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("SELECT");

        var limit = select.GetLimit();
        var offset = select.GetOffset();

        if (limit != null && offset == null)
            sb.Append($" TOP {limit}");

        AppendColumnsWithCalculated(sb, select);
        sb.Append(" FROM");
        AppendTable(sb, select);
        AppendWhere(sb, select, stringValueHandler);

        AppendGroupBy(sb, select);
        AppendOrderBy(sb, select, offset != null);

        if (offset != null)
        {
            sb.Append($" Offset {offset} Rows");
            if (limit != null)
                sb.Append($" Fetch NEXT {limit} ROWS ONLY");
        }

        return sb;
    }
    #endregion

    #region Insert
    public override string CompileInsertQuery(InsertQueryBuilder insert)
    {
        var sb = BuildInsertQuery(insert, null);
        return sb.ToString().Trim();
    }

    public override DbCommand CompileInsertCommand(InsertQueryBuilder insert)
    {
        var parameters = new List<DbParameter>();
        CustomStringValueHandler stringValueHandler = (QueryVal.StringVal stringVal) =>
        {
            var name = $"@p{parameters.Count}";
            var param = providerFactory.CreateParameter()!;
            param.ParameterName = name;
            param.Value = (string)stringVal.Value;
            parameters.Add(param);
            return name;
        };

        var sb = BuildInsertQuery(insert, stringValueHandler);

        var command = providerFactory.CreateCommand()!;
        command.CommandText = sb.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        return command;
    }
    private StringBuilder BuildInsertQuery(InsertQueryBuilder insert, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("INSERT INTO");
        AppendTable(sb, insert);
        AppendInsertColumns(sb, insert);
        sb.Append(" VALUES");
        AppendInsertValues(sb, insert, stringValueHandler);

        return sb;
    }
    #endregion

    #region Update
    public override string CompileUpdateQuery(UpdateQueryBuilder update)
    {
        var sb = BuildUpdateQuery(update, null);

        return sb.ToString().Trim();
    }
    public override DbCommand CompileUpdateCommand(UpdateQueryBuilder update)
    {
        var parameters = new List<DbParameter>();
        CustomStringValueHandler stringValueHandler = (QueryVal.StringVal stringVal) =>
        {
            var name = $"@p{parameters.Count}";
            var param = providerFactory.CreateParameter()!;
            param.ParameterName = name;
            param.Value = (string)stringVal.Value;
            parameters.Add(param);
            return name;
        };

        var sb = BuildUpdateQuery(update, stringValueHandler);

        var command = providerFactory.CreateCommand()!;
        command.CommandText = sb.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        return command;
    }
    private StringBuilder BuildUpdateQuery(UpdateQueryBuilder update, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("UPDATE");
        AppendTable(sb, update);
        AppendUpdate(sb, update, stringValueHandler);
        AppendWhere(sb, update, stringValueHandler);

        return sb;
    }
    #endregion

    #region Delete
    public override string CompileDeleteQuery(DeleteQueryBuilder delete)
    {
        var sb = BuildDeleteQuery(delete, null);
        return sb.ToString().Trim();
    }
    public override DbCommand CompileDeleteCommand(DeleteQueryBuilder delete)
    {
        var parameters = new List<DbParameter>();
        CustomStringValueHandler stringValueHandler = (QueryVal.StringVal stringVal) =>
        {
            var name = $"@p{parameters.Count}";
            var param = providerFactory.CreateParameter()!;
            param.ParameterName = name;
            param.Value = (string)stringVal.Value;
            parameters.Add(param);
            return name;
        };

        var sb = BuildDeleteQuery(delete, stringValueHandler);

        var command = providerFactory.CreateCommand()!;
        command.CommandText = sb.ToString();
        command.Parameters.AddRange(parameters.ToArray());
        return command;
    }
    private StringBuilder BuildDeleteQuery(DeleteQueryBuilder delete, CustomStringValueHandler? stringValueHandler)
    {
        var sb = new StringBuilder("DELETE FROM");
        AppendTable(sb, delete);
        AppendWhere(sb, delete, stringValueHandler);

        return sb;
    }
    #endregion



    private void AppendOrderBy(StringBuilder sb, IOrder select, bool hasOffset)
    {
        if (select.GetOrders().Count > 0)
        {
            var orders = select.GetOrders().Select(val => $"[{val.Key}] {(val.Value ? "ASC" : "DESC")}");
            sb.Append(" ORDER BY " + string.Join(", ", orders));
        }
        else if (hasOffset) // Offset needs a Order
            sb.Append(" ORDER BY (SELECT 0)");
    }

    private void AppendColumns(StringBuilder sb, IColumn query)
    {
        sb.Append(' ');
        if (query.GetColumns().Length == 0)
            sb.Append('*');
        else
        {
            var cols = query.GetColumns().Select(c => $"[{c}]");
            sb.AppendJoin(", ", cols);
        }
    }

    private void AppendColumnsWithCalculated(StringBuilder sb, ICalculatedColumn query)
    {
        sb.Append(' ');
        var cols = query.GetColumns();
        var calculated = query.GetCalculatedColumns();

        if (cols.Length == 0 && calculated.Length == 0)
            sb.Append('*');
        else
        {
            sb.AppendJoin(", ", calculated);
            sb.AppendJoin(", ", cols.Select(c => $"[{c}]"));
        }
    }

    private void AppendInsertColumns(StringBuilder sb, IColumn query)
    {
        if (query.GetColumns().Length == 0)
            return;

        sb.Append(" (");
        sb.AppendJoin(", ", query.GetColumns().Select(c => $"[{c}]"));
        sb.Append(')');
    }

    private void AppendTable(StringBuilder sb, ITable query)
        => sb.AppendFormat(" [{0}]", query.GetTable());

    private void AppendWhere(StringBuilder sb, IWhere query, CustomStringValueHandler? stringValueHandler)
    {
        var filterGroups = query.GetFilters();
        if (filterGroups?.Filters is null)
            return;

        sb.Append(" WHERE ");
        var parameters = new List<DbParameter>();
        AppendFilterGroup(sb, filterGroups, stringValueHandler);
        return;
    }

    private void AppendFilterGroup(StringBuilder sb, FilterGroup filterGroup, CustomStringValueHandler? stringValueHandler)
    {
        var current = filterGroup;

        while (current is not null)
        {
            var multiple = current.Filters?.Next is not null;
            if (multiple)
                sb.Append('(');

            var currentFilter = current.Filters;
            while (currentFilter != null)
            {
                var val = GetQueryString(currentFilter.Val, stringValueHandler);
                sb.AppendFormat("[{0}] {1} {2}", currentFilter.Col, currentFilter.Operation, val);

                if (currentFilter.Next is not null)
                {
                    sb.AppendFormat(" {0} ", currentFilter.Next.Value.op.ToString());
                    currentFilter = currentFilter.Next.Value.Item2;
                }
                else
                {
                    currentFilter = null;
                }
            }

            if (multiple)
                sb.Append(')');
            if (current.Next is not null)
            {
                sb.AppendFormat(" {0} ", current.Next.Value.op.ToString());
                current = current.Next.Value.Item2;
            }
            else
            {
                current = null;
            }
        }
    }

    private void AppendUpdate(StringBuilder sb, IUpdate query, CustomStringValueHandler? stringValueHandler)
    {
        sb.Append(" SET ");
        var updates = query
            .GetUpdates()
            .Select(kp =>
            {
                var val = GetQueryString(kp.Value, stringValueHandler);
                return $"[{kp.Key}] = {val}";
            });
        sb.AppendJoin(", ", updates);
    }

    private void AppendInsertValues(StringBuilder sb, IInsert query, CustomStringValueHandler? stringValueHandler)
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
                var val = GetQueryString(value, stringValueHandler);
                sb.Append(val);
            }

            sb.Append(')');
        }
    }

    private void AppendGroupBy(StringBuilder sb, IGroup query)
    {
        if (query.GetGroups().Length == 0) return;

        sb.Append(" GROUP BY ");
        var groups = query.GetGroups().Select(kp => $"[{kp}]");
        sb.AppendJoin(", ", groups);
    }
}