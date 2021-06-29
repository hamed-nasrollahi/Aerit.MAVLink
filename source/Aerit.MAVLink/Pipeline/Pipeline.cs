using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Aerit.MAVLink
{
    public class PipelineBuilder<T>
        where T : IMiddleware
    {
		internal PipelineBuilder(ILoggerFactory loggerFactory)
        {
			ILoggerFactory = loggerFactory;
		}

		internal ILoggerFactory ILoggerFactory { get; }

		internal T First { get; private set; }

        public (PipelineBuilder<T> builder, TNode last) Append<TNode>(Func<TNode> builder)
            where TNode : T
        {
            var node = builder();

			First = node;

			return (this, node);
        }

        public (PipelineBuilder<T> builder, TNode last) Append<TNode>(Func<ILogger<TNode>, TNode> builder)
            where TNode : T
        {
            var node = builder(ILoggerFactory.CreateLogger<TNode>());

			First = node;

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
        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(this (PipelineBuilder<T> builder, IBufferMiddlewareOutput last) node, Func<TNode> builder)
            where T : IMiddleware
            where TNode : IBufferMiddleware
        {
            var last = builder();

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(this (PipelineBuilder<T> builder, IBufferMiddlewareOutput last) node, Func<ILogger<TNode>, TNode> builder)
            where T : IMiddleware
            where TNode : IBufferMiddleware
        {
            var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node, Func<TNode> builder)
            where T : IMiddleware
            where TNode : IPacketMiddleware
        {
            var last = builder();

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode>(this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node, Func<ILogger<TNode>, TNode> builder)
            where T : IMiddleware
            where TNode : IPacketMiddleware
        {
            var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, PacketMapMiddleware last) Map<T>(this (PipelineBuilder<T> builder, IPacketMiddlewareOutput last) node, Action<PacketMapMiddleware> builder)
            where T : IMiddleware
        {
            var map = new PacketMapMiddleware(node.builder.ILoggerFactory);

			builder(map);

			node.last.Next = map;

            return (node.builder, map);
        }

        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode, TMessage>(this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node, Func<TNode> builder)
            where T : IMiddleware
            where TNode : IMessageMiddleware<TMessage>
        {
            var last = builder();

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, TNode last) Append<T, TNode, TMessage>(this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node, Func<ILogger<TNode>, TNode> builder)
            where T : IMiddleware
            where TNode : IMessageMiddleware<TMessage>
        {
            var last = builder(node.builder.ILoggerFactory.CreateLogger<TNode>());

            node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, MessageEndpoint<TMessage> last) Enpoint<T, TMessage>(this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node, Func<byte, byte, TMessage, bool> process)
            where T : IMiddleware
        {
			var last = new MessageEndpoint<TMessage>(process);

			node.last.Next = last;

            return (node.builder, last);
        }

        public static (PipelineBuilder<T> builder, MessageAsyncEndpoint<TMessage> last) Enpoint<T, TMessage>(this (PipelineBuilder<T> builder, IMessageMiddlewareOutput<TMessage> last) node, Func<byte, byte, TMessage, CancellationToken, Task<bool>> process)
            where T : IMiddleware
        {
			var last = new MessageAsyncEndpoint<TMessage>(process);

			node.last.Next = last;

            return (node.builder, last);
        }

        public static T Build<T, TNode>(this (PipelineBuilder<T> builder, TNode last) node)
            where T : IMiddleware
            where TNode : IMiddleware
            => node.builder.First;
    }
}