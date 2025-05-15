using System.Data.Common;

namespace DBQuery.QueryVals;

public record ParamVal(string ParameterName) : QueryVal((object)ParameterName)
{
    public ParamVal(DbParameter param) : this(param.ParameterName)
    {
    }
}