using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.NanoTask.Components;

[RegisterComponent]
public sealed partial class NanoTaskServerComponent : Component
{
    public List<NanoTaskItemAndDepartment> Tasks = [];
    public uint Counter;
}
