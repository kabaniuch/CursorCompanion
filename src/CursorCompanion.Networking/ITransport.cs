namespace CursorCompanion.Networking;

public interface ITransport
{
    bool IsConnected { get; }
    bool IsHost { get; }
    void Host(int port);
    void Connect(string ip, int port);
    void Send(byte[] data);
    void Disconnect();
    void PollEvents();
    event Action<byte[]>? OnReceive;
    event Action? OnPeerConnected;
    event Action? OnPeerDisconnected;
}
