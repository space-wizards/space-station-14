using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    [Dependency] private BodySystem _body = default!;

    /// <summary>
    ///     Macrocosm-specific initialization.
    /// </summary>
    private void InitializeMacrocosm()
    {
        // Relays
        SubscribeLocalEvent<BodyComponent, ReplaceOrganMarkingsEvent>(_body.RelayEvent);

        // Subscriptions
        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<ReplaceOrganMarkingsEvent>>(OnMarkingsOrganReplaceMarkings);
    }

    private void OnMarkingsOrganReplaceMarkings(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<ReplaceOrganMarkingsEvent> args)
    {
        if (Comp<OrganComponent>(ent).Category is not { } category)
            return;

        if (!args.Args.Markings.TryGetValue(category, out var markingSet))
            return;

        // We get a *complete* set of all layers, where the markings in markingSet are included,
        // and the rest are filled with empty lists.
        var completeSet = ent.Comp.MarkingData.Layers
            .ToDictionary(layer => layer,
                layer => markingSet.TryGetValue(layer, out var set)
                    ? set
                    : new List<Marking>());

        // Now we apply them - with empty groups included.
        // By adding the rest of the layer keys to the set with empty groups, the markings will be
        // replaced, rather than applied.
        ApplyVisualOrganMarkings(ent, completeSet);
    }

    /// <summary>
    ///     Replaces the entity's visual humanoid appearance with a given profile.
    /// </summary>
    /// <remarks>
    ///     Similar to <see cref="ApplyProfileTo"/>, but replaces markings instead.
    /// </remarks>
    /// <param name="ent">The body to replace the profile of</param>
    /// <param name="profile">The profile to replace it with</param>
    [PublicAPI]
    public void ReplaceProfileWith(Entity<VisualBodyComponent?> ent, HumanoidCharacterProfile profile)
    {
        ReplaceAppearanceWith(ent, profile.Appearance, profile.Sex);
    }

    /// <summary>
    ///     Replaces this entity's appearance with the given character appearance.
    /// </summary>
    /// <remarks>
    ///     Similar to <see cref="ApplyAppearanceTo"/>, but replaces markings instead.
    /// </remarks>
    private void ReplaceAppearanceWith(Entity<VisualBodyComponent?> ent, HumanoidCharacterAppearance appearance, Sex sex)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ApplyProfile(ent, new()
        {
            Sex = sex,
            SkinColor = appearance.SkinColor,
            EyeColor = appearance.EyeColor,
        });

        var markingsEvt = new ReplaceOrganMarkingsEvent(appearance.Markings);
        RaiseLocalEvent(ent, ref markingsEvt);
    }
}

/// <summary>
/// Raised on body entity when a profile is being applied to it
/// </summary>
[ByRefEvent]
public readonly record struct ReplaceOrganMarkingsEvent(Dictionary<ProtoId<OrganCategoryPrototype>,
    Dictionary<HumanoidVisualLayers, List<Marking>>> Markings);
