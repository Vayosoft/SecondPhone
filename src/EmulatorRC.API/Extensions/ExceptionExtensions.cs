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
            //SqlException => HandleSqlException((SqlException)exception, context, logger, correlationId),
            IOException ioException => HandleIOException(ioException, context, logger, correlationId),
            RpcException rpcException => HandleRpcException(rpcException, logger, correlationId),
            _ => HandleDefault(exception, context, logger, correlationId)
        };

        private static RpcException HandleIOException(IOException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            logger.LogWarning("gRPC {Method} CorrelationId: {CorrelationId} - The request stream was aborted.", context.Method, correlationId);
            return new RpcException(new Status(StatusCode.Aborted, exception.Message), CreateTrailers(correlationId));
        }

        private static RpcException HandleCancelledException(OperationCanceledException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            logger.LogWarning( "gRPC {Method} CorrelationId: {CorrelationId} - The operation was canceled.", context.Method, correlationId);
            return new RpcException(new Status(StatusCode.Cancelled, exception.Message), CreateTrailers(correlationId));
        }

        private static RpcException HandleTimeoutException(TimeoutException exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            logger.LogError(exception, "gRPC {Method} CorrelationId: {CorrelationId} - A timeout occurred.", context.Method, correlationId);
            var status = new Status(StatusCode.Internal, "An external resource did not answer within the time limit");

            return new RpcException(status, CreateTrailers(correlationId));
        }

        private static RpcException HandleRpcException(RpcException exception, ILogger logger, Guid correlationId)
        {
            logger.LogError(exception, "gRPC CorrelationId: {CorrelationId} - An error occurred.", correlationId);
            var trailers = exception.Trailers;
            trailers.Add(CreateTrailers(correlationId)[0]);
            return new RpcException(new Status(exception.StatusCode, exception.Message), trailers);
        }

        private static RpcException HandleDefault(Exception exception, ServerCallContext context, ILogger logger, Guid correlationId)
        {
            logger.LogError(exception, "gRPC {Method} CorrelationId: {CorrelationId} - An error occurred.", context.Method, correlationId);
            return new RpcException(new Status(StatusCode.Internal, exception.Message), CreateTrailers(correlationId));
        }

        //private static RpcException HandleSqlException<T>(SqlException exception, ServerCallContext context, ILogger<T> logger, Guid correlationId)
        //{
        //    logger.LogError(exception, $"CorrelationId: {correlationId} - An SQL error occurred");
        //    Status status;

        //    if (exception.Number == -2)
        //    {
        //        status = new Status(StatusCode.DeadlineExceeded, "SQL timeout");
        //    }
        //    else
        //    {
        //        status = new Status(StatusCode.Internal, "SQL error");
        //    }
        //    return new RpcException(status, CreateTrailers(correlationId));
        //}

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
