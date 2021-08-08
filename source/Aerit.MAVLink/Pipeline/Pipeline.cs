using System.Collections.Generic;

namespace Aerit.MAVLink
{
	public record Pipeline(
		IBufferMiddleware First,
		HashSet<uint> Ids
	);
}