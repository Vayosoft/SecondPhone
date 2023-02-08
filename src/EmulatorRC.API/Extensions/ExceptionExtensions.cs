using Grpc.Core;

namespace EmulatorRC.API.Extensions
{
    public static class ExceptionExtensions
    {
        public static RpcException Handle<T>(this Exception exception, ServerCallContext context, ILogger<T> logger, Guid correlationId) =>
        exception switch
        {
            OperationCanceledException canceledException => HandleCancelledException(canceledException, context, logger, correlationId),
            TimeoutException timeoutException => HandleTimeoutException(timeoutException, context, logger, correlationId),
            IOException ioException => HandleIoException(ioException, context, logger, correlationId),
            RpcException rpcException => HandleRpcException(rpcException, logger, correlationId),
            _ => HandleDefault(exception, context, logger, correlationId)
        };

        private static RpcException HandleIoException(IOException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("gRPC {RequestPath} CorrelationId: {CorrelationId} - The request stream was aborted.",
                    context.Method, correlationId);
            }

            return new RpcException(new Status(StatusCode.Aborted, exception.Message), CreateTrailers(correlationId));
        }

        private static RpcException HandleCancelledException(OperationCanceledException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("gRPC {RequestPath} CorrelationId: {CorrelationId} - The operation was canceled.",
                    context.Method, correlationId);
            }

            return new RpcException(new Status(StatusCode.Cancelled, exception.Message), CreateTrailers(correlationId));
        }

        private static RpcException HandleTimeoutException(TimeoutException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "gRPC {RequestPath} CorrelationId: {CorrelationId} - A timeout occurred.",
                    context.Method, correlationId);
            }

            var status = new Status(StatusCode.Internal, "An external resource did not answer within the time limit");

            return new RpcException(status, CreateTrailers(correlationId));
        }

        private static RpcException HandleRpcException(RpcException exception, ILogger logger, Guid correlationId)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(exception, "gRPC CorrelationId: {CorrelationId} - An error occurred.", correlationId);
            }

            var metadata = new Metadata {CreateTrailers(correlationId)[0]};
            foreach (var exceptionTrailer in exception.Trailers)
            {
                metadata.Add(exceptionTrailer);
            }
            return new RpcException(new Status(exception.StatusCode, exception.Message), metadata);
        }

        private static RpcException HandleDefault(Exception exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            logger.LogError(exception, "gRPC {RequestPath} CorrelationId: {CorrelationId} - An error occurred.", context.Method, correlationId);

            return new RpcException(new Status(StatusCode.Internal, exception.Message), CreateTrailers(correlationId));
        }

        /// <summary>
        ///  Adding the correlation to Response Trailers
        /// </summary>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        private static Metadata CreateTrailers(Guid correlationId)
        {
            var trailers = new Metadata {{"CorrelationId", correlationId.ToString()}};
            return trailers;
        }
    }
}
