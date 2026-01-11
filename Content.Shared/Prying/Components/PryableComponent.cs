using Content.Shared.Prying.Systems;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Prying.Components;

/// <summary>
/// As used, allows an entity to be pried open with a tool, with DoAfter time modifiers that can be influenced by the
/// user, the tool being used, and the state of the entity itself.
/// Generically, any tool quality can be used, and the events raised by <see cref="PryingSystem"/> can be customized
/// to create behavior that isn't just "open thing".
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(PryingSystem))]
public sealed partial class PryableComponent : Component
{
    /// <summary>
    /// What quality should a tool have in order to pry this entity?
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    /// <summary>
    /// Default time it takes to pry the entity.
    /// </summary>
    [DataField]
    public TimeSpan PryTime = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    /// Loc string to use for the added pry verb.
    /// </summary>
    [DataField]
    public LocId VerbLocStr = "door-pry";
}
