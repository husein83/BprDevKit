namespace BprDevKit.Utility
{
    /// <summary>
    /// Result type for operations across the application
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string AlertCssClass => Success ? "alert-success" : "alert-danger";
        public string IconCssClass => Success ? "bi-check-circle-fill" : "bi-x-octagon-fill";

        public static OperationResult SuccessResult(string message)
            => new() { Success = true, Message = message };

        public static OperationResult ErrorResult(string message, Exception? ex = null)
            => new() { Success = false, Message = message, Exception = ex };

        public static OperationResult<T> SuccessResult<T>(T data, string message)
            => OperationResult<T>.SuccessResult(data, message);

        public static OperationResult<T> ErrorResult<T>(string message, Exception? ex = null, T? data = default)
            => OperationResult<T>.ErrorResult(message, ex, data);
    }

    /// <summary>
    /// Generic result type carrying strongly-typed data payload
    /// </summary>
    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; set; }

        public static OperationResult<T> SuccessResult(T data, string message)
            => new() { Success = true, Message = message, Data = data };

        public new static OperationResult<T> ErrorResult(string message, Exception? ex = null)
            => new() { Success = false, Message = message, Exception = ex, Data = default };

        public static OperationResult<T> ErrorResult(string message, Exception? ex, T? data)
            => new() { Success = false, Message = message, Exception = ex, Data = data };

        /// <summary>
        /// Implicit conversion to tuple (OperationResult, T?) for 100% backward compatibility
        /// with existing code expecting tuple return types.
        /// </summary>
        public static implicit operator (OperationResult operationResult, T? data)(OperationResult<T> result)
            => (result, result.Data);
    }
}
