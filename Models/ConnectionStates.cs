namespace NightreignRelicEditor.Models;

public enum ConnectionStates
{
    NotConnected,
    NightreignNotFound,
    EACDetected,
    ConnectedOffsetsNotFound,
    Connected,
    ConnectionLost,
}