namespace Klang.Common.Voice.Exceptions;

public class NotInVoiceChannelException : Exception
{
    public NotInVoiceChannelException() {}

    public NotInVoiceChannelException(string message) : base(message) {}
    
    public NotInVoiceChannelException(string message, Exception innerException) : base(message, innerException) {}
}
