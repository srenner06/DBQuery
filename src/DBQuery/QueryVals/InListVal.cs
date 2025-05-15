namespace DBQuery.QueryVals;

public record InListVal(IEnumerable<QueryVal> Values) : QueryVal((object)Values.ToArray())
{
}