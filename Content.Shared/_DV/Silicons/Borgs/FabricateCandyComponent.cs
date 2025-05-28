using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Silicons.Borgs;

/// <summary>
/// Lets a medical borg spawn a number of candy items using actions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FabricateCandyComponent : Component
{
    /// <summary>
    /// Actions to add to the entity.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Actions = new();

    /// <summary>
    /// The sound played when fabricating candy.
    /// </summary>
    [DataField]
    public SoundSpecifier FabricationSound = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg")
    {
        Params = new AudioParams
        {
            Volume = -2f
        }
    };
}

/// <summary>
/// Action event to use for candy fabrication actions.
/// </summary>
public sealed partial class FabricateCandyActionEvent : InstantActionEvent
{
    /// <summary>
    /// The item to spawn at the borg.
    /// </summary>
    /// <remarks>
    /// The client only sends a <see cref="RequestPerformActionEvent"/>, no exploits possible with this being in the event.
    /// </remarks>
    [DataField(required: true)]
    public EntProtoId Item;
}
