using System;

using FASTER.core;

namespace Aerit.MAVLink.Store
{
	public class IndexStoreVariableLengthStruct : IVariableLengthStruct<Memory<byte>, long>
	{
		public static readonly IndexStoreVariableLengthStruct Instance = new();

		public int GetInitialLength(ref long input)
			=> 2 * sizeof(int) + sizeof(long);

		public int GetLength(ref Memory<byte> t, ref long input)
			=> sizeof(int) + t.Length + sizeof(long);
	}
}