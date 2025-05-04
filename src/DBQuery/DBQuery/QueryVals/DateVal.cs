namespace DBQuery.QueryVals;

public record DateVal(DateOnly DateValue) : QueryVal((object)DateValue)
{
}
