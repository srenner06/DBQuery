using System.Numerics;

namespace DBQuery.QueryVals;

public record NumericVal<T>(T NumericValue) : QueryVal(NumericValue) where T : INumber<T>
{
}