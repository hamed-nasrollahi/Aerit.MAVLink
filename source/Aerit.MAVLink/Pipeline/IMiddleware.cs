#nullable enable

using System.Collections.Generic;

namespace Aerit.MAVLink
{
	public interface IMiddleware
	{
		IEnumerable<uint>? Ids { get; }
	}
}