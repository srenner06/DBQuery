namespace DBQuery.QueryVals;

public record BetweenVal(QueryVal From, QueryVal To) : QueryVal((object)(From, To))
{
}

