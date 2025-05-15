namespace DBQuery.QueryVals;

public record StringVal(string StringValue) : QueryVal((object)StringValue)
{
}