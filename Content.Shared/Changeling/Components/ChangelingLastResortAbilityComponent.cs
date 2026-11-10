using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Allows a changeling to gib their current body and escape as a head slug.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingLastResortAbilityComponent : Component
{
    /// <summary>
    /// The prototype ID of the slug to spawn.
    /// </summary>
    [DataField]
    public EntProtoId SlugPrototype = "MobHeadSlug";

    /// <summary>
    /// The sound to play when the changeling gibbs.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("gib", AudioParams.Default.WithVariation(0.025f));
}

/// <summary>
/// Action event for Last Resort.
/// </summary>
public sealed partial class ChangelingLastResortActionEvent : InstantActionEvent;
