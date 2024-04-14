using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Construction.Components;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DisguiseToggleNoRotEvent>(OnToggleNoRot);
        SubscribeLocalEvent<ChameleonDisguiseComponent, DisguiseToggleAnchoredEvent>(OnToggleAnchored);
    }

    private void OnEquippedHand(Entity<ChameleonDisguiseComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!TryComp<PolymorphedEntityComponent>(ent, out var poly))
            return;

        _polymorph.Revert((ent, poly));
        args.Handled = true;
    }

    public override void Disguise(ChameleonProjectorComponent proj, EntityUid user, EntityUid entity)
    {
        if (_polymorph.PolymorphEntity(user, proj.Polymorph) is not {} disguise)
            return;

        // make disguise look real (for simple things at least)
        var meta = MetaData(entity);
        _meta.SetEntityName(disguise, meta.EntityName);
        _meta.SetEntityDescription(disguise, meta.EntityDescription);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.SourceEntity = entity;
        comp.SourceProto = Prototype(entity)?.ID;
        Dirty(disguise, comp);

        // no sechud trolling
        RemComp<StatusIconComponent>(disguise);

        _appearance.CopyData(entity, disguise);

        var mass = CompOrNull<PhysicsComponent>(entity)?.Mass ?? 0f;

        // let the disguise die when its taken enough damage, which then transfers to the player
        // health is proportional to mass, and capped to not be insane
        if (TryComp<MobThresholdsComponent>(disguise, out var thresholds))
        {
            // if the player is of flesh and blood, cap max health to theirs
            // so that when reverting damage scales 1:1 and not round removing
            var playerMax = _mobThreshold.GetThresholdForState(user, MobState.Dead).Float();
            var max = playerMax == 0f ? proj.MaxHealth : Math.Max(proj.MaxHealth, playerMax);

            var health = Math.Clamp(mass, proj.MinHealth, proj.MaxHealth);
            _mobThreshold.SetMobStateThreshold(disguise, health, MobState.Critical, thresholds);
            _mobThreshold.SetMobStateThreshold(disguise, max, MobState.Dead, thresholds);
        }

        // add actions for controlling transform aspects
        _actions.AddAction(disguise, proj.NoRotAction);
        _actions.AddAction(disguise, proj.AnchorAction);
    }

    private void OnToggleNoRot(Entity<ChameleonDisguiseComponent> ent, ref DisguiseToggleNoRotEvent args)
    {
        var xform = Transform(ent);
        xform.NoLocalRotation = !xform.NoLocalRotation;
    }

    private void OnToggleAnchored(Entity<ChameleonDisguiseComponent> ent, ref DisguiseToggleAnchoredEvent args)
    {
        var uid = ent.Owner;
        var xform = Transform(uid);
        if (xform.Anchored)
            _xform.Unanchor(uid, xform);
        else
            _xform.AnchorEntity((uid, xform));
    }
}
