using LiteNetLib;
using CursorCompanion.Core;

namespace CursorCompanion.Networking;

public class DirectUdpTransport : ITransport, INetEventListener
{
    private NetManager? _netManager;
    private NetPeer? _peer;
    private bool _isHost;

    // Rate limiting
    private int _actionPingCount;
    private float _rateLimitTimer;
    private const int MaxPingsPerWindow = 5;
    private const float RateLimitWindow = 10f;

    public bool IsConnected => _peer?.ConnectionState == ConnectionState.Connected;
    public bool IsHost => _isHost;

    private event Action<byte[]>? _onReceive;
    private event Action? _onPeerConnected;
    private event Action? _onPeerDisconnected;

    event Action<byte[]>? ITransport.OnReceive
    {
        add => _onReceive += value;
        remove => _onReceive -= value;
    }

    event Action? ITransport.OnPeerConnected
    {
        add => _onPeerConnected += value;
        remove => _onPeerConnected -= value;
    }

    event Action? ITransport.OnPeerDisconnected
    {
        add => _onPeerDisconnected += value;
        remove => _onPeerDisconnected -= value;
    }

    public void Host(int port)
    {
        _isHost = true;
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            DisconnectTimeout = 10000
        };
        _netManager.Start(port);
        Logger.Info($"Hosting on port {port}");
    }

    public void Connect(string ip, int port)
    {
        _isHost = false;
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            DisconnectTimeout = 10000
        };
        _netManager.Start();
        _netManager.Connect(ip, port, "CursorCompanion");
        Logger.Info($"Connecting to {ip}:{port}");
    }

    public void Send(byte[] data)
    {
        if (_peer == null) return;

        // Rate limit ActionPing messages
        if (data.Length > 0 && (MessageType)data[0] == MessageType.ActionPing)
        {
            if (_actionPingCount >= MaxPingsPerWindow)
            {
                Logger.Warn("ActionPing rate limited");
                return;
            }
            _actionPingCount++;
        }

        _peer.Send(data, DeliveryMethod.ReliableOrdered);
    }

    public void Disconnect()
    {
        _peer?.Disconnect();
        _netManager?.Stop();
        _peer = null;
        _netManager = null;
        Logger.Info("Disconnected");
    }

    public void PollEvents()
    {
        _netManager?.PollEvents();

        // Rate limit window
        _rateLimitTimer += Time.DeltaTime;
        if (_rateLimitTimer >= RateLimitWindow)
        {
            _rateLimitTimer = 0;
            _actionPingCount = 0;
        }
    }

    // INetEventListener implementations
    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        _peer = peer;
        Logger.Info($"Peer connected: {peer.Address}");
        _onPeerConnected?.Invoke();

        // Send hello
        var hello = MessageSerializer.Serialize(MessageType.Hello, new HelloMessage());
        peer.Send(hello, DeliveryMethod.ReliableOrdered);
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _peer = null;
        Logger.Info($"Peer disconnected: {disconnectInfo.Reason}");
        _onPeerDisconnected?.Invoke();
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        var data = reader.GetRemainingBytes();
        _onReceive?.Invoke(data);
    }

    void INetEventListener.OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Logger.Error($"Network error: {socketError} from {endPoint}");
    }

    void INetEventListener.OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    { }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        if (_isHost)
            request.AcceptIfKey("CursorCompanion");
        else
            request.Reject();
    }
}
