using Grpc.Core;

namespace Vayosoft.gRPC.Reactive;

public static class AsyncStreamReaderObservableExtensions
{
    public static IObservable<T> AsObservable<T>(
        this IAsyncStreamReader<T> reader,
        CancellationToken cancellationToken = default) =>
        new GrpcStreamObservable<T>(reader, cancellationToken);
}
