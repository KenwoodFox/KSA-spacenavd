using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KSA.Spacenavd;

internal sealed class SpacenavClient
{
    private const string SocketPath = "/var/run/spnav.sock";
    private const int EventSize = 32;

    private readonly object _gate = new();
    private readonly Thread _thread;
    private volatile bool _running;
    private Socket? _socket;
    private DebugState _state = new("starting", 0, 0, 0, 0, 0, 0, "none");

    public readonly record struct DebugState(
        string Status,
        int Tx,
        int Ty,
        int Tz,
        int Rx,
        int Ry,
        int Rz,
        string Button);

    public DebugState Snapshot()
    {
        lock (_gate)
            return _state;
    }

    public SpacenavClient()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "spacenav",
        };
    }

    public void Start()
    {
        _running = true;
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        try
        {
            _socket?.Shutdown(SocketShutdown.Both);
        }
        catch (SocketException)
        {
        }

        _socket?.Close();

        if (_thread.IsAlive)
            _thread.Join(TimeSpan.FromSeconds(2));
    }

    private void Run()
    {
        try
        {
            using var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _socket = sock;
            sock.Connect(new UnixDomainSocketEndPoint(SocketPath));
            SetStatus($"connected to {SocketPath}");

            var buf = new byte[EventSize];
            while (_running)
            {
                int n = 0;
                while (n < EventSize)
                {
                    int read = sock.Receive(buf, n, EventSize - n, SocketFlags.None);
                    if (read == 0)
                    {
                        SetStatus("socket closed");
                        return;
                    }

                    n += read;
                }

                var ev = MemoryMarshal.Cast<byte, int>(buf);
                lock (_gate)
                {
                    if (ev[0] == 0)
                    {
                        _state = _state with
                        {
                            Tx = ev[1],
                            Ty = ev[2],
                            Tz = ev[3],
                            Rx = ev[4],
                            Ry = ev[5],
                            Rz = ev[6],
                        };
                    }
                    else
                    {
                        _state = _state with
                        {
                            Button = $"{ev[1]} {(ev[0] == 1 ? "pressed" : "released")}",
                        };
                    }
                }
            }
        }
        catch (Exception ex) when (!_running)
        {
            SetStatus($"stopped ({ex.GetType().Name})");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void SetStatus(string status)
    {
        lock (_gate)
            _state = _state with { Status = status };
    }
}
