using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Dragon;

[NetworkedComponent, EntityCategory("Spawner")]
public abstract partial class SharedDragonRiftComponent : Component
{
    [DataField("state")]
    public DragonRiftState State = DragonRiftState.Charging;
}

[Serializable, NetSerializable]
public enum DragonRiftState : byte
{
    Charging,
    AlmostFinished,
    Finished,
}
