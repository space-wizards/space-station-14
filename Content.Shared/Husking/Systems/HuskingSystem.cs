using Content.Shared.Body;
using Content.Shared.Cloning;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Husking.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Mobs;
using Content.Shared.Rejuvenate;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Husking.Systems;

/// <summary>
/// Handles logic regarding the <see cref="HuskedComponent"/>.
/// Marks entities as husked, hiding their identity until healed.
/// </summary>
public sealed class HuskingSystem : EntitySystem
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private static readonly ProtoId<CloningSettingsPrototype> BaseCloningSettings = "Body";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HuskedComponent, SeeIdentityAttemptEvent>(OnSeeIdentity);
        SubscribeLocalEvent<HuskedComponent, ExaminedEvent>(OnHuskExamined);

        SubscribeLocalEvent<HuskedComponent, ComponentShutdown>(OnHuskedShutdown);
        SubscribeLocalEvent<HuskedComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HuskedComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnMobStateChanged(Entity<HuskedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Alive)
            return;

        Unhusk(ent.Owner);
    }

    private void OnRejuvenate(Entity<HuskedComponent> ent, ref RejuvenateEvent args)
    {
        Unhusk(ent.Owner);
    }

    private void OnHuskedShutdown(Entity<HuskedComponent> ent, ref ComponentShutdown args)
    {
        _identity.QueueIdentityUpdate(ent.Owner);

        if (ent.Comp.OriginalAppearance == null)
            return;

        _visualBody.CopyAppearanceFrom(ent.Comp.OriginalAppearance.Value, ent.Owner);
        QueueDel(ent.Comp.OriginalAppearance);

        Unhusk(ent.Owner);
    }

    private void OnSeeIdentity(Entity<HuskedComponent> ent, ref SeeIdentityAttemptEvent args)
    {
        var species = Loc.GetString("husking-corpse-husked-species-backup");
        if (TryComp<HumanoidProfileComponent>(ent, out var humanoid) && _protoMan.TryIndex(humanoid.Species, out var speciesPrototype))
            species = Loc.GetString(speciesPrototype.Name).ToLower();

        args.NameOverride = Loc.GetString("husking-corpse-husked", ("species", species));
        args.TotalCoverage |= IdentityBlockerCoverage.FULL;
    }

    private void OnHuskExamined(Entity<HuskedComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("husking-corpse-examine", ("target", Identity.Entity(ent, EntityManager))));
    }

    /// <summary>
    /// Husks the target, hiding their identity and causing them to take on a default appearance.
    /// Only works on valid humanoids with a valid species.
    /// </summary>
    /// <param name="target">The entity to husk.</param>
    public bool TryHusk(EntityUid target)
    {
        if (!TryComp<HumanoidProfileComponent>(target, out var humanoid)
            || !_protoMan.TryIndex(humanoid.Species, out var speciesPrototype))
            return false;

        if (!_cloning.TryCloning(target, MapCoordinates.Nullspace, BaseCloningSettings, out var clone))
            return false;

        var comp = EnsureComp<HuskedComponent>(target);
        comp.OriginalAppearance = clone;

        var huskedBodyAppearance = Spawn(speciesPrototype.Prototype, MapCoordinates.Nullspace);
        _visualBody.CopyAppearanceFrom(huskedBodyAppearance, target);
        QueueDel(huskedBodyAppearance); // We only care about it for the initial clone.
        _identity.QueueIdentityUpdate(target);

        return true;
    }

    /// <summary>
    /// Removes the husking effect from an entity, restoring their identity and appearance to what it was previously.
    /// </summary>
    /// <param name="ent">The entity to unhusk.</param>
    public void Unhusk(Entity<HuskedComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        RemCompDeferred<HuskedComponent>(ent);
    }
}
