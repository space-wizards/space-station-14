using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking;
using Content.Server.Store.Systems;
using Content.Shared.Alert;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Eye;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedInteractionSystem _interact = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevenantComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<RevenantComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<RevenantComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RevenantComponent, StatusEffectAddedEvent>(OnStatusAdded);
        SubscribeLocalEvent<RevenantComponent, StatusEffectEndedEvent>(OnStatusEnded);
        SubscribeLocalEvent<RoundEndMessageEvent>(_ => MakeVisible(true));

        SubscribeLocalEvent<RevenantComponent, GetVisMaskEvent>(OnRevenantGetVis);

        InitializeAbilities();
    }

    private void OnRevenantGetVis(Entity<RevenantComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Ghost;
    }

    private void OnStartup(EntityUid uid, RevenantComponent component, ComponentStartup args)
    {
        //default the visuals
        _appearance.SetData(uid, RevenantVisuals.Corporeal, false);
        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
        _appearance.SetData(uid, RevenantVisuals.Stunned, false);

        if (_ticker.RunLevel == GameRunLevel.PostRound && TryComp<VisibilityComponent>(uid, out var visibility))
        {
            _visibility.AddLayer((uid, visibility), (int) VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((uid, visibility), (int) VisibilityFlags.Normal, false);
            _visibility.RefreshVisibility(uid, visibility);
        }

        //ghost vision
        _eye.RefreshVisibilityMask(uid);
    }

    private void OnStatusAdded(EntityUid uid, RevenantComponent component, StatusEffectAddedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, RevenantVisuals.Stunned, true);
    }

    private void OnStatusEnded(EntityUid uid, RevenantComponent component, StatusEffectEndedEvent args)
    {
        if (args.Key == "Stun")
            _appearance.SetData(uid, RevenantVisuals.Stunned, false);
    }

    private void OnExamine(EntityUid uid, RevenantComponent component, ExaminedEvent args)
    {
        if (args.Examiner == args.Examined)
        {
            if (!TryComp<SatiationComponent>(uid, out var satiationComp))
                return;

            var essence = _satiation.GetValueOrNull((uid, satiationComp), component.EssenceSatiationPrototype);
            var regenCap = _satiation.GetThresholdValueOrNull((uid, satiationComp), component.EssenceSatiationPrototype, component.MaxRegenThreshold);

            if (essence == null || regenCap == null)
                return;

            args.PushMarkup(Loc.GetString("revenant-essence-amount",
                ("current", (int)essence!), ("max", (int)regenCap!)));
        }
    }

    private void OnDamage(EntityUid uid, RevenantComponent component, DamageChangedEvent args)
    {
        if (!HasComp<CorporealComponent>(uid) || args.DamageDelta == null)
            return;

        var essenceDamage = args.DamageDelta.GetTotal().Float() * component.DamageToEssenceCoefficient * -1;
        if (!TryComp<SatiationComponent>(uid, out var satiationComp))
            return;

        _satiation.ModifyValue((uid, satiationComp), component.EssenceSatiationPrototype, (float)essenceDamage);

        if (_satiation.IsValueInRange((uid, satiationComp), component.EssenceSatiationPrototype, below: component.MinEssenceThreshold))
        {
            Spawn(component.SpawnOnDeathPrototype, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }

    public void MakeVisible(bool visible)
    {
        var query = EntityQueryEnumerator<RevenantComponent, VisibilityComponent>();
        while (query.MoveNext(out var uid, out _, out var vis))
        {
            if (visible)
            {
                _visibility.AddLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                _visibility.RemoveLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
            }
            else
            {
                _visibility.AddLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                _visibility.RemoveLayer((uid, vis), (int) VisibilityFlags.Normal, false);
            }
            _visibility.RefreshVisibility(uid, vis);
        }
    }

    /// <summary>
    /// Cools the area around the revenant.
    /// The more essence they have, the colder it gets, up to a certain point.
    /// </summary>
    /// <param name="ent">The revenant entity.</param>
    private void ChillArea(Entity<RevenantComponent> ent)
    {
        if (!TryComp<SatiationComponent>(ent, out var satiationComp))
            return;
        var essence = _satiation.GetValueOrNull((ent, satiationComp), ent.Comp.EssenceSatiationPrototype);
        if (essence == null)
            return;

        var effectiveEssence = Math.Clamp((int)essence, 0, ent.Comp.ChillUpperBound.Float());
        // Parabolic curve based on essence, more essence = more delta q, flattening as upper bound is reached
        var dQ =
            200 / ent.Comp.ChillScaling.Float() * MathF.Pow(effectiveEssence - ent.Comp.ChillUpperBound.Float(), 2) -
            ent.Comp.ChillScaling.Float();

        if (_atmosphere.GetContainingMixture(ent.Owner, true, true) is { } air)
            _atmosphere.AddHeat(air, dQ);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RevenantComponent>();
        while (query.MoveNext(out var uid, out var rev))
        {
            ChillArea((uid, rev));
        }
    }
}
