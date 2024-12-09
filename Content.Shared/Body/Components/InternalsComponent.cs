using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Components;

/// <summary>
/// Handles hooking up a mask (breathing tool) / gas tank together and allowing the Owner to breathe through it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class InternalsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? GasTankEntity;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> BreathTools = new();

    /// <summary>
    /// Toggle Internals delay when the target is not you.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField]
    public ProtoId<AlertPrototype> InternalsAlert = "Internals";
}
