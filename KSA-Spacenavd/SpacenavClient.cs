using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace KSA.Spacenavd;

internal sealed class SpacenavClient
{
    private const string SocketPath = "/var/run/spnav.sock";
    private const int EventSize = 32;

    private readonly Thread _thread;
    private volatile bool _running;
    private Socket? _socket;

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
            Console.WriteLine($"KSA-Spacenavd: connected to {SocketPath}");

            var buf = new byte[EventSize];
            while (_running)
            {
                int n = 0;
                while (n < EventSize)
                {
                    int read = sock.Receive(buf, n, EventSize - n, SocketFlags.None);
                    if (read == 0)
                    {
                        Console.WriteLine("KSA-Spacenavd: socket closed");
                        return;
                    }

                    n += read;
                }

                var ev = MemoryMarshal.Cast<byte, int>(buf);
                if (ev[0] == 0)
                {
                    Console.WriteLine(
                        $"KSA-Spacenavd: motion T=({ev[1]}, {ev[2]}, {ev[3]}) R=({ev[4]}, {ev[5]}, {ev[6]})");
                }
                else
                {
                    Console.WriteLine(
                        $"KSA-Spacenavd: button {ev[1]} {(ev[0] == 1 ? "pressed" : "released")}");
                }
            }
        }
        catch (Exception ex) when (!_running)
        {
            Console.WriteLine($"KSA-Spacenavd: stopped ({ex.GetType().Name})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KSA-Spacenavd: {ex.Message}");
        }
    }
}
