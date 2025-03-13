using Content.Server._Impstation.CosmicCult.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.Portable;
using Content.Server.Bible.Components;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Components.Examine;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server._Impstation.CosmicCult;

public sealed class CosmicGlyphSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly CosmicCultRuleSystem _cultRule = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicGlyphComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<CosmicGlyphComponent, ActivateInWorldEvent>(OnUseGlyph);
        SubscribeLocalEvent<CosmicGlyphConversionComponent, TryActivateGlyphEvent>(OnConversionGlyph);
        SubscribeLocalEvent<CosmicGlyphAstralProjectionComponent, TryActivateGlyphEvent>(OnAstralProjGlyph);
        SubscribeLocalEvent<CosmicGlyphTransmuteWeaponComponent, TryActivateGlyphEvent>(OnTransmuteWeaponGlyph);
        SubscribeLocalEvent<CosmicGlyphTransmuteArmorComponent, TryActivateGlyphEvent>(OnTransmuteArmorGlyph);
        SubscribeLocalEvent<CosmicGlyphTransmuteSpireComponent, TryActivateGlyphEvent>(OnTransmuteSpireGlyph);
    }

    #region Base trigger

    private void OnExamine(Entity<CosmicGlyphComponent> uid, ref ExaminedEvent args)
    {
        if (HasComp<CosmicCultComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString("cosmic-examine-glyph-cultcount", ("COUNT", uid.Comp.RequiredCultists)));
            switch (uid.Comp.GlyphName) // This seems like the most straightforward way to do this, rather than making seperate examine events for each glyph component
            {
                case "knowledge":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-knowledge"));
                    break;
                case "truth":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-truth"));
                    break;
                case "cessation":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-cessation"));
                    break;
                case "blades":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-blades"));
                    break;
                case "warding":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-warding"));
                    break;
                case "projection":
                    args.PushMarkup(Loc.GetString("cosmic-examine-glyph-projection"));
                    break;
            }
        }
        else
            args.PushMarkup(Loc.GetString("cosmic-examine-text-glyphs"));
    }

    private void OnUseGlyph(Entity<CosmicGlyphComponent> uid, ref ActivateInWorldEvent args)
    {
        Log.Debug($"Glyph event triggered!");
        var tgtpos = Transform(uid).Coordinates;
        var userCoords = Transform(args.User).Coordinates;
        if (args.Handled || !userCoords.TryDistance(EntityManager, tgtpos, out var distance) || distance > uid.Comp.ActivationRange || !HasComp<CosmicCultComponent>(args.User))
            return;
        var cultists = GatherCultists(uid, uid.Comp.ActivationRange);
        if (cultists.Count < uid.Comp.RequiredCultists)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-not-enough-cultists"), uid, args.User);
            return;
        }

        args.Handled = true;
        var damageSpecifier = new DamageSpecifier();
        var tryInvokeEv = new TryActivateGlyphEvent(args.User, cultists);
        RaiseLocalEvent(uid, tryInvokeEv);
        if (tryInvokeEv.Cancelled)
            return;

        damageSpecifier.DamageDict.Add("Asphyxiation", uid.Comp.ActivationDamage / cultists.Count);
        foreach (var cultist in cultists)
        {
            DealDamage(cultist, damageSpecifier);
        }

        _audio.PlayPvs(uid.Comp.GylphSFX, tgtpos, AudioParams.Default.WithVolume(+1f));
        Spawn(uid.Comp.GylphVFX, tgtpos);
        QueueDel(uid);
    }
    #endregion

    #region Conversion
    private void OnConversionGlyph(Entity<CosmicGlyphConversionComponent> uid, ref TryActivateGlyphEvent args)
    {
        var possibleTargets = GetTargetsNearGlyph(uid, uid.Comp.ConversionRange, entity => HasComp<CosmicCultComponent>(entity));
        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-conditions-not-met"), uid, args.User);
            args.Cancel();
            return;
        }
        if (possibleTargets.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-too-many-targets"), uid, args.User);
            args.Cancel();
            return;
        }

        foreach (var target in possibleTargets) // FIVE GODDAMN if-statements? Yep. I know. Why? My brain doesn't have enough juice to write something more succinct.
        {
            if (_mobState.IsDead(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-dead"), uid, args.User);
                args.Cancel();
            }
            else if (uid.Comp.NegateProtection == false && HasComp<BibleUserComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-chaplain"), uid, args.User);
                args.Cancel();
            }
            else if (uid.Comp.NegateProtection == false && HasComp<MindShieldComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("cult-glyph-target-mindshield"), uid, args.User);
                args.Cancel();
            }
            else
            {
                _stun.TryStun(target, TimeSpan.FromSeconds(4f), false);
                _damageable.TryChangeDamage(target, uid.Comp.ConversionHeal * -1);
                _cultRule.CosmicConversion(target);
            }
        }
    }
    #endregion

    #region Astral Projection
    private void OnAstralProjGlyph(Entity<CosmicGlyphAstralProjectionComponent> uid, ref TryActivateGlyphEvent args)
    {
        _damageable.TryChangeDamage(args.User, uid.Comp.ProjectionDamage, true);
        var projectionEnt = Spawn(uid.Comp.SpawnProjection, Transform(uid).Coordinates);
        if (_mind.TryGetMind(args.User, out var mindId, out var _))
            _mind.TransferTo(mindId, projectionEnt);
        EnsureComp<CosmicMarkBlankComponent>(args.User);
        EnsureComp<CosmicAstralBodyComponent>(projectionEnt, out var astralComp);
        var mind = Comp<MindComponent>(mindId);
        mind.PreventGhosting = true;
        astralComp.OriginalBody = args.User;
        _stun.TryKnockdown(args.User, TimeSpan.FromSeconds(2), true);
    }
    #endregion

    #region Transmute Weapon
    private void OnTransmuteWeaponGlyph(Entity<CosmicGlyphTransmuteWeaponComponent> uid, ref TryActivateGlyphEvent args)
    {
        var tgtpos = Transform(uid).Coordinates;
        var possibleTargets = GatherSharpItems(uid, uid.Comp.TransmuteRange);
        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-conditions-not-met"), uid, args.User);
            args.Cancel();
            return;
        }
        if (possibleTargets.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-too-many-targets"), uid, args.User);
            args.Cancel();
            return;
        }
        foreach (var target in possibleTargets)
        {
            Spawn(_random.Pick(uid.Comp.TransmuteWeapon), tgtpos);
            QueueDel(target);
        }
    }
    #endregion

    #region Transmute Armor
    private void OnTransmuteArmorGlyph(Entity<CosmicGlyphTransmuteArmorComponent> uid, ref TryActivateGlyphEvent args)
    {
        var tgtpos = Transform(uid).Coordinates;
        var possibleTargets = GatherPressureSuitItems(uid, uid.Comp.TransmuteRange);
        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-conditions-not-met"), uid, args.User);
            args.Cancel();
            return;
        }
        if (possibleTargets.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-too-many-targets"), uid, args.User);
            args.Cancel();
            return;
        }
        foreach (var target in possibleTargets)
        {
            Spawn(uid.Comp.TransmuteArmor, tgtpos);
            QueueDel(target);
        }
    }
    #endregion

    #region Transmute Spire
    private void OnTransmuteSpireGlyph(Entity<CosmicGlyphTransmuteSpireComponent> uid, ref TryActivateGlyphEvent args)
    {
        var tgtpos = Transform(uid).Coordinates;
        var possibleTargets = GatherPortableScrubbers(uid, uid.Comp.TransmuteRange);
        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-conditions-not-met"), uid, args.User);
            args.Cancel();
            return;
        }
        if (possibleTargets.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("cult-glyph-too-many-targets"), uid, args.User);
            args.Cancel();
            return;
        }
        foreach (var target in possibleTargets)
        {
            Spawn(uid.Comp.TransmuteSpire, tgtpos);
            QueueDel(target);
        }
    }
    #endregion


    #region Housekeeping
    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;
        // So the original DamageSpecifier will not be changed.
        var newDamage = new DamageSpecifier(damage);
        _damageable.TryChangeDamage(user, newDamage, true);
    }

    /// <summary>
    ///     Gets all cultists/constructs near a glyph.
    /// </summary>
    public HashSet<EntityUid> GatherCultists(EntityUid uid, float range)
    {
        var entities = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, range);
        entities.RemoveWhere(entity => !HasComp<CosmicCultComponent>(entity) || _container.IsEntityInContainer(entity));
        return entities;
    }

    /// <summary>
    ///     Gets all sharp items near a glyph.
    /// </summary>
    private HashSet<EntityUid> GatherSharpItems(EntityUid uid, float range)
    {
        var items = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, range);
        items.RemoveWhere(item => !HasComp<SharpComponent>(item) || _container.IsEntityInContainer(item));
        return items;
    }

    /// <summary>
    ///     Gets all portablescrubbers near a glyph.
    /// </summary>
    private HashSet<EntityUid> GatherPortableScrubbers(EntityUid uid, float range)
    {
        var items = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, range);
        items.RemoveWhere(item => !HasComp<PortableScrubberComponent>(item) || _container.IsEntityInContainer(item));
        return items;
    }

    /// <summary>
    ///     Gets all items with clothing movespeed modifier and pressure protection near a glyph.
    /// </summary>
    private HashSet<EntityUid> GatherPressureSuitItems(EntityUid uid, float range)
    {
        var items = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, range);
        items.RemoveWhere(item => !HasComp<ClothingSpeedModifierComponent>(item) || !HasComp<PressureProtectionComponent>(item) || _container.IsEntityInContainer(item));
        return items;
    }

    /// <summary>
    ///     Gets all the humanoids near a glyph.
    /// </summary>
    /// <param name="uid">The glyph.</param>
    /// <param name="range">Radius for a lookup.</param>
    /// <param name="exclude">Filter to exclude from return.</param>
    private HashSet<Entity<HumanoidAppearanceComponent>> GetTargetsNearGlyph(EntityUid uid, float range, Predicate<Entity<HumanoidAppearanceComponent>>? exclude = null)
    {
        var possibleTargets = _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(Transform(uid).Coordinates, range);
        if (exclude != null)
            possibleTargets.RemoveWhere(exclude);
        possibleTargets.RemoveWhere(target => HasComp<CosmicMarkBlankComponent>(target) || HasComp<CosmicMarkLapseComponent>(target)); // We never want these.

        return possibleTargets;
    }
    #endregion
}
