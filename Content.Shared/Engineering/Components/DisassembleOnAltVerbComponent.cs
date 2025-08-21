using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Engineering.Components;

/// <summary>
///     Add a verb to entities that will disassemble them after an optional doafter to a specified prototype.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DisassembleOnAltVerbComponent : Component
{
    /// <summary>
    ///     The prototype that is spawned after disassembly. If null, nothing will spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? PrototypeToSpawn;

    /// <summary>
    ///     The time it takes to disassemble the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DisassembleTime = TimeSpan.FromSeconds(0);
}

[Serializable, NetSerializable]
public sealed partial class DisassembleDoAfterEvent : SimpleDoAfterEvent;
