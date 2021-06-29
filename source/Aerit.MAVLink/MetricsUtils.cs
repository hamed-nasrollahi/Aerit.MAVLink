using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Prometheus;

namespace Aerit.MAVLink
{
	public static class MetricsUtils
	{
		public static async Task<string> ExportAsync(CancellationToken token = default)
		{
			using var stream = new MemoryStream();

			await Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream, token);

			stream.Seek(0, SeekOrigin.Begin);

			using var reader = new StreamReader(stream);

			return reader.ReadToEnd();
		}
	}
}