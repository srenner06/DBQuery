using System.Data.Common;
using System.Numerics;

namespace DBQuery;

public abstract class QueryVal(object value)
{
    public object Value { get; set; } = value;
    public static QueryVal Null => new NullVal();
    public abstract string GetString();
    public override string ToString() => GetString();

    public static explicit operator string(QueryVal val) => val.GetString();

    public static implicit operator QueryVal(string text) => String(text);

    public static implicit operator QueryVal(DateTime val) => Date(val);

    public static implicit operator QueryVal(int val) => Number(val);
    public static implicit operator QueryVal(long val) => Number(val);
    public static implicit operator QueryVal(ulong val) => Number(val);
    public static implicit operator QueryVal(double val) => Number(val);

    public static implicit operator QueryVal(bool val) => Bool(val);

    public static implicit operator QueryVal(DbParameter val) => Param(val);

    public static QueryVal String(string text) => new StringVal(text);
    public static QueryVal Date(DateTime date) => new DateVal(date);
    public static QueryVal Number<T>(T num) where T : INumber<T> => new NumericVal<T>(num);
    public static QueryVal Bool(bool val) => new BoolVal(val);
    public static QueryVal Param(string name) => new ParamVal(name);
    public static QueryVal Param(DbParameter param) => new ParamVal(param);
    public static QueryVal Default() => new DefaultVal();
    public static QueryVal Computed(string query) => new ComputedVal(query);
    public static QueryVal Object(object obj) => new StringVal(obj.ToString()!);

    public class StringVal(string val) : QueryVal(val)
    {
        public override string GetString() => $"'{val}'";

        public static explicit operator StringVal(string val) => new(val);
    }

    public class DateVal(DateTime val) : QueryVal(val)
    {
        public override string GetString() => $"'{val:YYYY-MM-DD hh:mm:ss}'";
    }

    public class NumericVal<T>(T val) : QueryVal(val) where T : INumber<T>
    {
        public override string GetString() => $"{val}";
    }

    public class BoolVal(bool val) : QueryVal(val)
    {
        public override string GetString() => val ? "1" : "0";
    }

    public class ParamVal(string name) : QueryVal(name)
    {
        public ParamVal(DbParameter param) : this(param.ParameterName)
        {
        }

        public override string GetString() => name;
    }

    public class NullVal() : QueryVal(null!)
    {
        public override string GetString() => "NULL";
    }

    public class DefaultVal() : QueryVal(null!)
    {
        public override string GetString() => "DEFAULT";
    }

    public class ComputedVal(string query) : QueryVal(query)
    {
        public override string GetString() => $"({query})";
    }
}