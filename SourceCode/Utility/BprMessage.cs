namespace BprDevKit.Utility
{
    public sealed class BprMessage
    {
        public string Message { get; }

        public MessageType MessageType { get; }

        public DateTime Timestamp { get; } = DateTime.UtcNow;

        public BprMessage(string message, MessageType messageType = MessageType.Information)
        {
            Message = message;
            MessageType = messageType;
        }

        public string AlertCssClass => MessageType switch
        {
            MessageType.Success => "alert-success",
            MessageType.Error => "alert-danger",
            MessageType.Warning => "alert-warning",
            MessageType.Information => "alert-info",
            MessageType.YesOrNo => "alert-primary",
            _ => "alert-info"
        };

        public string IconCssClass => MessageType switch
        {
            MessageType.Success => "bi-check-circle-fill",
            MessageType.Error => "bi-exclamation-octagon-fill",
            MessageType.Warning => "bi-exclamation-triangle-fill",
            MessageType.Information => "bi-info-circle-fill",
            MessageType.YesOrNo => "bi-question-circle-fill",
            _ => "bi-info-circle-fill"
        };

        public string BadgeCssClass => MessageType switch
        {
            MessageType.Success => "text-bg-success",
            MessageType.Error => "text-bg-danger",
            MessageType.Warning => "text-bg-warning",
            MessageType.Information => "text-bg-info",
            MessageType.YesOrNo => "text-bg-primary",
            _ => "text-bg-secondary"
        };

        public bool IsSuccess => MessageType == MessageType.Success;
        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public static BprMessage Success(string message) => new(message, MessageType.Success);
        public static BprMessage Error(string message) => new(message, MessageType.Error);
        public static BprMessage Warning(string message) => new(message, MessageType.Warning);
        public static BprMessage Information(string message) => new(message, MessageType.Information);
        public static BprMessage Info(string message) => new(message, MessageType.Information);

        public static BprMessage FromResult(OperationResult result)
            => new(result.Message, result.Success ? MessageType.Success : MessageType.Error);

        public static implicit operator BprMessage?(OperationResult? result)
            => result == null ? null : FromResult(result);
    }
}
