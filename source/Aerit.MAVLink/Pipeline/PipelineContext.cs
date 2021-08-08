using System.Collections.Generic;

namespace Aerit.MAVLink
{
	public record PipelineContext(
		(byte? systemId, MavComponent? componentId) Target,
		HashSet<uint> Ids 
	);
}