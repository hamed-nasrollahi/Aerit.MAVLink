using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink
{
	public class PipelineBuilder<T>
		where T : IMiddleware
	{
		public PipelineBuilder(ILoggerFactory loggerFactory)
		{
			ILoggerFactory = loggerFactory;
		}

		internal ILoggerFactory ILoggerFactory { get; }

		internal T First { get; private set; }

		internal HashSet<uint> Ids { get; } = new();

		internal void RegisterIds(IEnumerable<uint> ids)
		{
			if (ids is null)
			{
				return;
			}

			foreach (var id in ids)
			{
				Ids.Add(id);
			}
		}

		public (PipelineBuilder<T> builder, TNode last) Append<TNode>(Func<TNode> builder)
			where TNode : T
		{
			var node = builder();

			First = node;
			RegisterIds(node.Ids);

			return (this, node);
		}

		public (PipelineBuilder<T> builder, TNode last) Append<TNode>()
			where TNode : T, new()
			=> Append(() => new TNode());

		public (PipelineBuilder<T> builder, TNode last) Append<TNode>(Func<ILogger<TNode>, TNode> builder)
			where TNode : T
		{
			var node = builder(ILoggerFactory.CreateLogger<TNode>());

			First = node;
			RegisterIds(node.Ids);

			return (this, node);
		}
	}

	public static class PipelineBuilder
	{
		public static PipelineBuilder<IBufferMiddleware> Create(ILoggerFactory loggerFactory)
			=> new(loggerFactory);
	}

	public static class BranchBuilder
	{
		public static PipelineBuilder<IPacketMapBranch> Create(ILoggerFactory loggerFactory)
			=> new(loggerFactory);
	}

	public static class PipelineBuilderExtensions
	{
		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(
			this (PipelineBuilder<T> builder, IBufferMiddlewareOutput last) node,
			Func<TNode> builder)
			where T : IMiddleware
			where TNode : IBufferMiddleware
		{
			var last = builder();

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(
			this (PipelineBuilder<T> builder, IBufferMiddlewareOutput last) node,
			Func<ILogger<TNode>, TNode> builder)
			where T : IMiddleware
			where TNode : IBufferMiddleware
		{
			var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(
			this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node,
			Func<TNode> builder)
			where T : IMiddleware
			where TNode : IPacketMiddleware
		{
			var last = builder();

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(
			this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node,
			Func<ILogger<TNode>, TNode> builder)
			where T : IMiddleware
			where TNode : IPacketMiddleware
		{
			var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, PacketMapMiddleware last) Map<T>(
			this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node,
			Action<PacketMapMiddleware> builder)
			where T : IMiddleware
		{
			var map = new PacketMapMiddleware(node.builder.ILoggerFactory);

			builder(map);

			node.last.Next = map;
			node.builder.RegisterIds(map.Ids);

			return (node.builder, map);
		}

		public static (PipelineBuilder<IBufferMiddleware> builder, FilterBufferMiddleware last) UseFilter(
			this PipelineBuilder<IBufferMiddleware> builder,
			bool target = true,
			bool ids = true)
			=> builder
				.Append(() => new FilterBufferMiddleware(target, ids));

		public static (PipelineBuilder<IBufferMiddleware> builder, PacketValidationMiddleware last) UsePacket(this PipelineBuilder<IBufferMiddleware> builder)
			=> builder
				.Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
				.Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger));

		public static (PipelineBuilder<IBufferMiddleware> builder, PacketValidationMiddleware last) UsePacket<TNode>(
			this (PipelineBuilder<IBufferMiddleware> builder, TNode last) node)
			where TNode : IBufferMiddlewareOutput
			=> node
				.Append((ILogger<PacketMiddleware> logger) => new PacketMiddleware(logger))
				.Append((ILogger<PacketValidationMiddleware> logger) => new PacketValidationMiddleware(logger));

		public static (PipelineBuilder<IBufferMiddleware> builder, BufferEndpoint last) Endpoint(
			this PipelineBuilder<IBufferMiddleware> builder,
			Func<ReadOnlyMemory<byte>, PipelineContext, bool> process)
			=> builder
				.Append(() => new BufferEndpoint(process));

		public static (PipelineBuilder<IBufferMiddleware> builder, BufferAsyncEndpoint last) Endpoint(
			this PipelineBuilder<IBufferMiddleware> builder,
			Func<ReadOnlyMemory<byte>, PipelineContext, CancellationToken, Task<bool>> process)
			=> builder
				.Append(() => new BufferAsyncEndpoint(process));

		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode, TMessage>(
			this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node,
			Func<TNode> builder)
				where T : IMiddleware
				where TNode : IMessageMiddleware<TMessage>
		{
			var last = builder();

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode, TMessage>(
			this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node,
			Func<ILogger<TNode>, TNode> builder)
			where T : IMiddleware
			where TNode : IMessageMiddleware<TMessage>
		{
			var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, MessageEndpoint<TMessage> last) Endpoint<T, TMessage>(
			this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node,
			Func<byte, byte, TMessage, bool> process)
			where T : IMiddleware
		{
			var last = new MessageEndpoint<TMessage>(process);

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, MessageAsyncEndpoint<TMessage> last) Endpoint<T, TMessage>(
			this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node,
			Func<byte, byte, TMessage, CancellationToken, Task<bool>> process)
			where T : IMiddleware
		{
			var last = new MessageAsyncEndpoint<TMessage>(process);

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static (PipelineBuilder<T> builder, LogMessageEndpoint<TMessage> last) Log<T, TMessage>(
			this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node)
			where T : IMiddleware
		{
			var last = new LogMessageEndpoint<TMessage>(node.builder.ILoggerFactory.CreateLogger<LogMessageEndpoint<TMessage>>());

			node.last.Next = last;
			node.builder.RegisterIds(last.Ids);

			return (node.builder, last);
		}

		public static IPacketMapBranch Build<TNode>(this (PipelineBuilder<IPacketMapBranch> builder, TNode last) node)
			where TNode : IMiddleware
			=> node.builder.First;

		public static Pipeline Build<T, TNode>(this (PipelineBuilder<T> builder, TNode last) node)
			where T : IBufferMiddleware
			where TNode : IMiddleware
			=> new(node.builder.First, node.builder.Ids);
	}
}