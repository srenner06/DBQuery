namespace DBQuery.QueryVals;

public record BoolVal(bool BoolValue) : QueryVal((object)BoolValue)
{
}