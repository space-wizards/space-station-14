using Content.Shared.Actions;
using Content.Shared.CosmicCult.Components;
using Content.Shared.Dataset;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.CosmicCult;

public abstract partial class CosmicRiftSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly EntProtoId PressureImmunityEffect = "StatusEffectPressureImmunity";

    [SubscribeLocalEvent]
    protected virtual void OnCollide(Entity<CosmicRiftComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.HitTimer != null)
            return;

        if (!HasComp<CosmicLambaParticleComponent>(args.OtherEntity) || !TryComp<ProjectileComponent>(args.OtherEntity, out var projectile) || !HasComp<CosmicLambdaDeviceComponent>(projectile.Shooter))
            return;

        _color.RaiseEffect(Color.FromHex("#9F18FF"), new List<EntityUid>() { ent }, Filter.Pvs(ent, entityManager: EntityManager));
        var random = SharedRandomExtensions.PredictedRandom(Timing, GetNetEntity(ent));
        var text = GetText(ent.Comp.PopUpDataset, random);

        if (random.Prob(ent.Comp.TextChance) && !string.IsNullOrWhiteSpace(text))
            _popup.PopupEntity(Loc.GetString(text), ent, PopupType.Medium);

        ent.Comp.CurrentHits++;
        ent.Comp.HitTimer = Timing.CurTime + TimeSpan.FromSeconds(0.5f);
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<CosmicRiftComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!TryComp<CosmicCultistComponent>(args.User, out var cultist) || args.Handled)
            return;

        if (ent.Comp.Occupied)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-inuse"), args.User, args.User);
            return;
        }

        if (cultist.WasEmpowered)
        {
            _popup.PopupEntity(Loc.GetString("cosmiccult-rift-cannotabsorb"), args.User, args.User);
            return;
        }

        args.Handled = true;
        ent.Comp.Occupied = true;
        _popup.PopupEntity(Loc.GetString("cosmiccult-rift-beginabsorb"), args.User, args.User);
        var doargs = new DoAfterArgs(EntityManager, args.User, ent.Comp.AbsorbTime, new EventAbsorbRiftDoAfter(), args.User, ent)
        {
            MovementThreshold = 0.5f, DistanceThreshold = 1.5f, Hidden = true, BreakOnDamage = true, BreakOnHandChange = true, BreakOnMove = true,
        };
        _doAfter.TryStartDoAfter(doargs);
    }

    [SubscribeLocalEvent]
    private void OnAbsorbDoAfter(Entity<CosmicCultistComponent> ent, ref EventAbsorbRiftDoAfter args)
    {
        var comp = ent.Comp;
        if (args.Args.Target is not { } target || args.Cancelled || args.Handled)
        {
            if (TryComp<CosmicRiftComponent>(args.Args.Target, out var rift))
                rift.Occupied = false;
            return;
        }
        args.Handled = true;

        _actions.AddAction(ent, ent.Comp.CosmicFragmentationAction);
        Spawn(CosmicCultSystem.GenericVfx, Transform(target).Coordinates);

        var ev = new CosmicCultistEmpowerChangedEvent(ent, true);
        RaiseLocalEvent(ent, ref ev);

        comp.WasEmpowered = true;
        _statusEffects.TrySetStatusEffectDuration(ent, PressureImmunityEffect);
        _popup.PopupCoordinates(Loc.GetString("cosmiccult-rift-absorb", ("NAME", Identity.Entity(args.Args.User, EntityManager))), Transform(args.Args.User).Coordinates, PopupType.MediumCaution);
        QueueDel(target);
    }

    private string? GetText(ProtoId<LocalizedDatasetPrototype> dialogue, IRobustRandom random)
    {
        if (!ProtoMan.Resolve(dialogue, out var proto))
            return null;

        return random.Pick(proto.Values);
    }
}

[ByRefEvent]
public record struct CosmicRiftPurgeEvent(bool Cancelled = false);
