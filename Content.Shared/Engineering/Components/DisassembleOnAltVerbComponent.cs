using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Engineering.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DisassembleOnAltVerbComponent : Component
{
    /// <summary>
    ///     The prototype that is spawned after disassembly. If null, nothing will spawn.
    /// </summary>
    [DataField]
    public ProtoId<EntityPrototype>? PrototypeToSpawn;

    /// <summary>
    ///     The time it takes to disassemble the entity.
    /// </summary>
    [DataField]
    public TimeSpan DisassembleTime = TimeSpan.FromSeconds(0);
}

[Serializable, NetSerializable]
public sealed partial class DisassembleDoAfterEvent : SimpleDoAfterEvent { }
