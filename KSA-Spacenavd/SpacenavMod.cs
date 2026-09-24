using Brutal.ImGuiApi;
using Brutal.Numerics;
using StarMap.API;

namespace KSA.Spacenavd;

[StarMapMod]
public sealed class SpacenavMod
{
    private SpacenavClient? _client;
    private string? _guiError;

    [StarMapBeforeMain]
    public void OnBeforeMain()
    {
        Console.WriteLine("KSA-Spacenavd: loaded");
        _client = new SpacenavClient();
        _client.Start();
    }

    [StarMapAfterGui]
    public void OnAfterGui(double dt)
    {
        try
        {
            var pos = new float2(80, 80);
            var size = new float2(420, 160);
            ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
            ImGui.SetNextWindowPos(in pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(in size, ImGuiCond.Always);
            ImGui.Begin("KSA-Spacenavd", ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoSavedSettings);

            var state = _client?.Snapshot();
            if (state is null)
            {
                ImGui.Text("not started");
            }
            else
            {
                ImGui.Text(state.Value.Status);
                ImGui.Text($"T  {state.Value.Tx}  {state.Value.Ty}  {state.Value.Tz}");
                ImGui.Text($"R  {state.Value.Rx}  {state.Value.Ry}  {state.Value.Rz}");
                ImGui.Text($"button  {state.Value.Button}");
            }

            ImGui.End();
        }
        catch (Exception ex) when (_guiError != ex.Message)
        {
            _guiError = ex.Message;
            Console.WriteLine($"KSA-Spacenavd UI: {ex}");
        }
    }

    [StarMapUnload]
    public void Unload()
    {
        _client?.Stop();
        _client = null;
        Console.WriteLine("KSA-Spacenavd: unloaded");
    }
}
