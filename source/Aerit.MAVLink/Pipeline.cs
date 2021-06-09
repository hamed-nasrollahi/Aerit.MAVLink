using System;

namespace Aerit.MAVLink
{
    public static class PipelineBuilder
    {
        public static (IBufferMiddleware first, T last) Append<T>(Func<T> builder)
            where T : IBufferMiddleware
        {
            var first = builder();

            return (first, first);
        }
    }

    public static class BranchBuilder
    {
        public static (IPacketMapBranch first, T last) Append<T>(Func<T> builder)
            where T : IPacketMapBranch
        {
            var first = builder();

            return (first, first);
        }
    }

    public static class PipelineBuilderExtensions
    {
        public static T1 Build<T1, T2>(this (T1 first, T2 last) node)
            where T1 : IMiddleware
            => node.first;

        public static (T1 first, T2 last) Append<T1, T2>(this (T1 first, IBufferMiddlewareOutput last) node, Func<T2> builder)
            where T1 : IMiddleware
            where T2 : IBufferMiddleware
        {
            var last = builder();

            node.last.Next = last;

            return (node.first, last);
        }

        public static (T1 first, T2 last) Append<T1, T2>(this (T1 first, IPacketMiddlewareOutput last) node, Func<T2> builder)
            where T1 : IMiddleware
            where T2 : IPacketMiddleware
        {
            var last = builder();

            node.last.Next = last;

            return (node.first, last);
        }

        public static (T1 first, T2 last) Append<T1, T2, T3>(this (T1 first, IMessageMiddlewareOutput<T3> last) node, Func<T2> builder)
            where T1 : IMiddleware
            where T2 : IMessageMiddleware<T3>
        {
            var last = builder();

            node.last.Next = last;

            return (node.first, last);
        }
    }
}