using Content.Shared.Alert;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class CuffableComponent : Component
{
    public const string DefaultState = "humanoid";

    /// <summary>
    /// The current RSI for the handcuff layer
    /// </summary>
    [ViewVariables]
    public string? CurrentRSI;

    /// <summary>
    /// How many of this entity's hands are currently cuffed.
    /// </summary>
    [ViewVariables]
    public bool Cuffed => Container.ContainedEntities.Count > 0;

    /// <summary>
    ///     Container of various handcuffs currently applied to the entity.
    /// </summary>
    [ViewVariables]
    public Container Container = default!;

    /// <summary>
    /// Optional override for our sprite state when cuffed. Will only be used if the cuff supports that state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? State;

    /// <summary>
    /// Whether or not the entity can still interact (is not cuffed)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanStillInteract = true;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> CuffedAlert = "Handcuffed";
}

public sealed partial class RemoveCuffsAlertEvent : BaseAlertEvent;

[ByRefEvent]
public readonly record struct CuffedStateChangeEvent;

