using System.Data.Common;
using System.Numerics;

namespace DBQuery.QueryVals;

public abstract record QueryVal(object? Value)
{
    public static QueryVal Null => new NullVal();

    public static implicit operator QueryVal(string text) => String(text);
    public static implicit operator QueryVal(DateTime val) => DateTime(val);
    public static implicit operator QueryVal(DateOnly val) => DateOnly(val);
    public static implicit operator QueryVal(int val) => Number(val);
    public static implicit operator QueryVal(long val) => Number(val);
    public static implicit operator QueryVal(ulong val) => Number(val);
    public static implicit operator QueryVal(double val) => Number(val);
    public static implicit operator QueryVal(bool val) => Bool(val);
    public static implicit operator QueryVal(DbParameter val) => Param(val);

    public static QueryVal String(string text) => new StringVal(text);
    public static QueryVal DateTime(DateTime date) => new DateTimeVal(date);
    public static QueryVal DateOnly(DateOnly date) => new DateVal(date);
    public static QueryVal Number<T>(T num) where T : INumber<T> => new NumericVal<T>(num);
    public static QueryVal Bool(bool val) => new BoolVal(val);
    public static QueryVal Param(string name) => new ParamVal(name);
    public static QueryVal Default() => new DefaultVal();
    public static QueryVal Computed(string query) => new ComputedVal(query);
    public static QueryVal Param(DbParameter param) => new ParamVal(param);
    public static QueryVal Object(object obj) => new StringVal(obj.ToString()!);
    public static QueryVal In(params QueryVal[] values) => new InListVal(values);
    public static QueryVal Between(QueryVal from, QueryVal to) => new BetweenVal(from, to);

}
