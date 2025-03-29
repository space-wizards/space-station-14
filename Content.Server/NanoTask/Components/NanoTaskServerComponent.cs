using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.NanoTask.Components;

[RegisterComponent]
public sealed partial class NanoTaskServerComponent : Component
{
    public Dictionary<string, List<NanoTaskItemAndId>> DepartamentTasks = [];
    public List<NanoTaskItemAndId> StationTasks = [];
    public uint Counter;
}
