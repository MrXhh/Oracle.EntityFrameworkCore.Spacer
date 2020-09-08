namespace Oracle.EntityFrameworkCore
{
	internal enum OracleTraceTag
	{
		None = 0,
		Error = 0x10000000,
		Environment = 1,
		Version = 2,
		Config = 4,
		Sqlnet = 8,
		Tnsnames = 0x10,
		Entry = 0x100,
		Exit = 0x200,
		SQL = 0x400,
		Map = 0x800,
		Connection = 0x1000,
	}
}
