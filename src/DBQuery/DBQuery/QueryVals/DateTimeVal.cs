namespace DBQuery.QueryVals;

public record DateTimeVal(DateTime DateTimeValue) : QueryVal((object)DateTimeValue)
{
}
