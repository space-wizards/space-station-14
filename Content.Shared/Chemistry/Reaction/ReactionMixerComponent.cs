using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Reaction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ReactionMixerComponent : Component
{
    /// <summary>
    /// A list of IDs for categories of reactions that can be mixed (i.e. HOLY for a bible, DRINK for a spoon).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<MixingCategoryPrototype>> ReactionTypes = new();

    /// <summary>
    /// The popup message when successfully mixing a solution.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId MixMessage = "default-mixing-success";

    /// <summary>
    /// Defines if interacting is enough to mix with this component.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MixOnInteract = true;

    /// <summary>
    /// How long it takes to mix with this.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeToMix = TimeSpan.Zero;
}

[ByRefEvent]
public record struct MixingAttemptEvent(EntityUid Mixed, bool Cancelled = false);

[ByRefEvent]
public readonly record struct AfterMixingEvent(EntityUid Mixed, EntityUid Mixer);

[Serializable, NetSerializable]
public sealed partial class ReactionMixDoAfterEvent : SimpleDoAfterEvent;
