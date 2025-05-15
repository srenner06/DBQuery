namespace DBQuery.QueryVals;

public record BlobVal(byte[] Data) : QueryVal((object)Data)
{
}