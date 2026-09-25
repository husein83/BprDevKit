namespace BprDevKit.Utility
{
    /// <summary>
    /// Result type for database operations
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static OperationResult SuccessResult(string message)
            => new() { Success = true, Message = message };

        public static OperationResult ErrorResult(string message, Exception? ex = null)
            => new() { Success = false, Message = message, Exception = ex };
    }
}
