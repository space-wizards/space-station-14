using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

using Content.Shared.Singularity.EntitySystems;

namespace Content.Shared.Singularity.Components;

[NetworkedComponent]
public abstract class SharedSingularityComponent : Component
{
    // Shared
    [Access(friends:typeof(SharedSingularitySystem))]
    public ulong _level = 1;

    [DataField("level")]
    [ViewVariables(VVAccess.ReadWrite)]
    public ulong Level
    {
        get => _level;
        set { IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedSingularitySystem>().SetSingularityLevel(this, value); }
    }

    /// <summary>
    ///     The amount of radiation this singularity emits per its level.
    ///     Has to be on shared in case someone attaches a RadiationPulseComponent to the singularity.
    /// </summary>
    [DataField("radsPerLevel")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float RadsPerLevel = 1f;

    [Serializable, NetSerializable]
    public sealed class SingularityComponentState : ComponentState
    {
        public readonly ulong Level;

        public SingularityComponentState(ulong level)
        {
            Level = level;
        }
    }
}
