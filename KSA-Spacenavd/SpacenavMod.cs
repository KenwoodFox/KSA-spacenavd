using StarMap.API;

namespace KSA.Spacenavd;

[StarMapMod]
public sealed class SpacenavMod
{
    private SpacenavClient? _client;

    [StarMapBeforeMain]
    public void OnBeforeMain()
    {
        Console.WriteLine("KSA-Spacenavd: loaded");
        _client = new SpacenavClient();
        _client.Start();
    }

    [StarMapUnload]
    public void Unload()
    {
        _client?.Stop();
        _client = null;
        Console.WriteLine("KSA-Spacenavd: unloaded");
    }
}
