using Brutal.ImGuiApi;
using KSA;
using StarMap.API;
using StarMap.SimpleMod.Dependency;

namespace StarMap.SimpleMod2
{
    [StarMapMod]
    public class SimpleMod2
    {
        [StarMapAfterGui]
        public void OnAfterUi(double dt)
        {
            var dependency = new DependencyClass();

            ImGui.Begin("MyWindow");
            ImGui.Text($"Current Value: {DependencyForOtherMods.Value}");
            ImGui.Text(dependency.DoSomething());
            ImGui.End();
        }
    }
}
