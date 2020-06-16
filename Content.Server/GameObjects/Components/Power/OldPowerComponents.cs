using Content.Shared.GameObjects.Components.Power;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Power
{
    //Placeholder while map prototypes are fixed
    [RegisterComponent]
    public sealed class ApcComponent : SharedApcComponent { }
    [RegisterComponent]
    public class PowerDebugTool : SharedPowerDebugTool { }
    [RegisterComponent]
    public class PowerDeviceComponent : Component
    {
        public override string Name => "PowerDevice";
    }
    [RegisterComponent]
    public class PowerGeneratorComponent : Component
    {
        public override string Name => "PowerGenerator";
    }
    [RegisterComponent]
    public class PowerNodeComponent : Component
    {
        public override string Name => "PowerNode";
    }
    [RegisterComponent]
    public class PowerProviderComponent : PowerDeviceComponent
    {
        public override string Name => "PowerProvider";
    }
    [RegisterComponent]
    public class PowerStorageNetComponent : Component
    {
        public override string Name => "PowerStorage";
    }
    [RegisterComponent]
    public class PowerTransferComponent : Component
    {
        public override string Name => "PowerTransfer";
    }
}
