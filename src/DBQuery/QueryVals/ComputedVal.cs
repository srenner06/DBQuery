namespace DBQuery.QueryVals;

public record ComputedVal(string ComputedValue) : QueryVal((object)ComputedValue)
{
}