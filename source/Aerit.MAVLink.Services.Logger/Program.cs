using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aerit.MAVLink.Services.Logger
{
	using Store;

	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureServices((hostContext, services) =>
				{
					services.Configure<Store.Writer.Options>(hostContext.Configuration.GetSection("Store"));
					services.AddSingleton(provider =>
					{
						var writer = new Store.Writer(
							provider.GetRequiredService<ILogger<Store.Writer>>(),
							provider.GetRequiredService<IOptions<Store.Writer.Options>>());

						writer.Register(SourceIndexer.Instance);
						writer.Register(TargetIndexer.Instance);

						return writer;
					});

					services.Configure<UdpTransmissionChannelIn.Options>(hostContext.Configuration.GetSection("Transmission"));
					services.AddSingleton<UdpTransmissionChannelIn>();
					services.AddSingleton<ITransmissionChannel>(o => o.GetRequiredService<UdpTransmissionChannelIn>());

					services.Configure<Client.Options>(hostContext.Configuration.GetSection("Client"));
					services.AddSingleton<Client>();

					services.AddTransient<PipelineBuilder<IBufferMiddleware>>();

					services.AddHostedService<Worker>();
				});
	}
}