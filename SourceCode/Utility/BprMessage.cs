namespace BprDevKit.Utility
{
    public sealed class BprMessage
    {
        public readonly string Message;

        public readonly MessageType MessageType;

        public BprMessage(string message, MessageType messageType = MessageType.Information)
        {
            Message = message;
            MessageType = messageType;
        }
    }
}
