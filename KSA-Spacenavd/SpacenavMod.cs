using System.Reflection;
using System.Reflection.Emit;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using HarmonyLib;
using KSA;
using StarMap.API;

namespace KSA.Spacenavd;

[StarMapMod]
public sealed class SpacenavMod
{
    internal static bool ShowWindow;
    internal static int Deadband = 180;

    private Harmony? _harmony;
    private static SpacenavClient? _client;

    [StarMapBeforeMain]
    public void OnBeforeMain()
    {
        _harmony = new Harmony("KSA.Spacenavd");
        _harmony.PatchAll(typeof(SpacenavMod).Assembly);

        _client = new SpacenavClient();
        _client.Start();
    }

    [StarMapAfterGui]
    public void OnAfterGui(double dt)
    {
        if (!ShowWindow)
            return;

        var pos = new float2(80, 80);
        var size = new float2(420, 200);
        ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
        ImGui.SetNextWindowPos(in pos, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(in size, ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("KSA-Spacenavd", ref ShowWindow, ImGuiWindowFlags.NoDocking))
        {
            ImGui.End();
            return;
        }

        ImGui.SliderInt("Deadband"u8, ref Deadband, 0, 250);

        var state = _client?.Snapshot();
        if (state is null)
        {
            ImGui.Text("not started");
        }
        else
        {
            var flags = ToThrusterFlags(state.Value, Deadband);
            ImGui.Text(state.Value.Status);
            ImGui.Text($"T  {state.Value.Tx}  {state.Value.Ty}  {state.Value.Tz}");
            ImGui.Text($"R  {state.Value.Rx}  {state.Value.Ry}  {state.Value.Rz}");
            ImGui.Text($"RCS  {flags}");
        }

        ImGui.End();
    }

    [StarMapUnload]
    public void Unload()
    {
        _harmony?.UnpatchAll("KSA.Spacenavd");
        _harmony = null;
        _client?.Stop();
        _client = null;
    }

    internal static ThrusterMapFlags CurrentFlags()
    {
        var state = _client?.Snapshot();
        return state is null ? ThrusterMapFlags.None : ToThrusterFlags(state.Value, Deadband);
    }

    // spacenavd is Y-up, +Z toward the user. KSA RCS is flag-only (same as keyboard).
    internal static ThrusterMapFlags ToThrusterFlags(SpacenavClient.DebugState state, int deadband)
    {
        var flags = ThrusterMapFlags.None;
        if (state.Tx > deadband) flags |= ThrusterMapFlags.TranslateRight;
        if (state.Tx < -deadband) flags |= ThrusterMapFlags.TranslateLeft;
        if (state.Ty > deadband) flags |= ThrusterMapFlags.TranslateUp;
        if (state.Ty < -deadband) flags |= ThrusterMapFlags.TranslateDown;
        if (state.Tz > deadband) flags |= ThrusterMapFlags.TranslateForward;
        if (state.Tz < -deadband) flags |= ThrusterMapFlags.TranslateBackward;
        if (state.Rx > deadband) flags |= ThrusterMapFlags.PitchUp;
        if (state.Rx < -deadband) flags |= ThrusterMapFlags.PitchDown;
        if (state.Ry > deadband) flags |= ThrusterMapFlags.YawLeft;
        if (state.Ry < -deadband) flags |= ThrusterMapFlags.YawRight;
        if (state.Rz > deadband) flags |= ThrusterMapFlags.RollRight;
        if (state.Rz < -deadband) flags |= ThrusterMapFlags.RollLeft;
        return flags;
    }
}

[HarmonyPatch(typeof(VehicleUpdateState), nameof(VehicleUpdateState.PrepareFromVehicle))]
internal static class PrepareFromVehiclePatch
{
    private static void Postfix(VehicleUpdateState __instance)
    {
        if (!__instance.IsControlled || Program.IsEditorOpen)
            return;
        if (Universe.GetSimulationSpeed() > 30.0)
            return;

        var flags = SpacenavMod.CurrentFlags();
        if (flags == ThrusterMapFlags.None)
            return;

        var inputs = __instance.ManualControlInputs;
        inputs.ThrusterCommandFlags |= flags;
        __instance.ManualControlInputs = inputs;
    }
}

[HarmonyPatch(typeof(Program), nameof(Program.DrawMenuBar))]
internal static class DrawMenuBarPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var field = AccessTools.Field(typeof(Program), nameof(Program.ShowExperimentalParticles));
        var hook = AccessTools.Method(typeof(DrawMenuBarPatch), nameof(DrawMenuItem));
        var list = instructions.ToList();

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].opcode != OpCodes.Ldsflda || !Equals(list[i].operand, field))
                continue;

            for (var j = i + 1; j < list.Count && j < i + 8; j++)
            {
                if (list[j].opcode != OpCodes.Call || list[j].operand is not MethodInfo method || method.Name != "DrawMenuItem")
                    continue;

                list.Insert(j + 1, new CodeInstruction(OpCodes.Call, hook));
                return list;
            }
        }

        return list;
    }

    private static void DrawMenuItem()
    {
        ImGuiHelper.DrawMenuItem("Show Spacenavd"u8, ref SpacenavMod.ShowWindow);
    }
}
