using System.Data.Common;
using DBQuery.Compilers;
using DBQuery.QueryVals;
using static DBQuery.Compilers.QueryCompiler;

namespace DBQuery.QueryBuilders;

public abstract class BaseQueryBuilder<T>(QueryCompiler compiler) : ITable<T> where T : BaseQueryBuilder<T>
{
    protected string _tableName = "";

    public string GetTable() => _tableName;

    public virtual T SetTable(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            _tableName = name.Trim();
        return (T)this;
    }

    /// <summary>
    /// Gibs das Query als String zurück. <br></br>
    /// Wenn externe strings (wie User Input) verwendet werden ist SQL-Injection möglich.<br></br>
    /// Deshalb sollte in diesen Fällen <see cref="ToCommand"/> verwendet werden oder <paramref name="stringValueHandler"/> gesetzt werden
    /// </summary>
    /// <returns></returns>
    public string ToQuery(CustomStringValueHandler? stringValueHandler = null) => compiler.ToQuery((T)this, stringValueHandler);

    public DbCommand ToCommand() => compiler.ToCommand((T)this);
}

public enum JoinType
{
    Left,
    Right,
    Inner,
    FullOuter,
    Full,
    Cross
}
public class Join(string tableToJoin, JoinType type, string leftTable, string leftColumn, string rightTable, string rightColumn)
{
    public string TableToJoin = tableToJoin;
    public JoinType Type = type;
    public string LeftTable = leftTable;
    public string LeftColumn = leftColumn;
    public string RightTable = rightTable;
    public string RightColumn = rightColumn;
    public ValueOperator Operator;
}

public enum LogicalOperator
{
    And,
    Or
}
public enum ValueOperator
{
    Equals,
    NotEquals,
    Greater,
    Smaller,
    GreaterEquals,
    SmallerEquals,
    Like,
    In,
    NotIn,
    Between
}

public interface ICondition
{
    LogicalOperator JoinWithPrevious { get; set; }
}
public class FilterCondition(string column, QueryVal value, ValueOperator op, LogicalOperator logicalOp, string? table = null) : ICondition
{
    public string Column = column;
    public QueryVal Value = value;
    public ValueOperator Operator = op;
    public string? Table = table;
    public LogicalOperator JoinWithPrevious { get; set; } = logicalOp;
}

public class ConditionGroup(LogicalOperator op) : ICondition
{
    public LogicalOperator JoinWithPrevious { get; set; } = op;
    public List<ICondition> Conditions = [];
}