using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Mobs.Components;
using Content.Shared.Research.Prototypes;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Xenoborgs;

/// <summary>
/// A hybrid between <see cref="SharedMaterialReclaimerSystem"/> and <see cref="SharedLatheSystem"/> for streamlined production of cyborgs.
/// </summary>
public abstract class SharedXenoborgFactorySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CollideXenoborgFactoryComponent, StartCollideEvent>(OnCollide);
    }

    public void OnCollide(EntityUid uid, CollideXenoborgFactoryComponent component, ref StartCollideEvent args)
    {
        // if (args.OurFixtureId != component.FixtureId)
        //     return;
        if (!TryComp<XenoborgFactoryComponent>(uid, out var reclaimer))
            return;
        TryStartProcessItem(uid, args.OtherEntity, reclaimer);
    }

    /// <summary>
    /// Tries to start processing an item via a <see cref="XenoborgFactoryComponent"/>.
    /// </summary>
    private void TryStartProcessItem(EntityUid uid,
        EntityUid item,
        XenoborgFactoryComponent? component = null,
        EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (false) //todo: check if alive
            return;

        if (!HasComp<MobStateComponent>(item)) // todo: check if victim is borgable
            return;

        if (!CanProduce(uid, component))
            return;

        if (HasComp<BorgChassisComponent>(item))
            return;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, item) ||
            _whitelistSystem.IsBlacklistPass(component.Blacklist, item))
            return;

        if (Container.TryGetContainingContainer((item, null, null), out _) && !Container.TryRemoveFromContainer(item))
            return;

        if (user != null)
        {
            _adminLog.Add(LogType.Action,
                LogImpact.High,
                $"{ToPrettyString(user.Value):player} destroyed {ToPrettyString(item)} in the mothership core, {ToPrettyString(uid)}");
        }

        if (Timing.CurTime > component.NextSound)
        {
            component.Stream = _audio.PlayPredicted(component.Sound, uid, user)?.Entity;
            component.NextSound = Timing.CurTime + component.SoundCooldown;
        }

        var reclaimedEvent = new GotReclaimedEvent(Transform(uid).Coordinates);
        RaiseLocalEvent(item, ref reclaimedEvent);


        Reclaim(uid, item, component);
    }

    /// <summary>
    /// Spawns the materials and chemicals associated
    /// with an entity. Also deletes the item.
    /// </summary>
    public virtual void Reclaim(EntityUid uid, EntityUid item, XenoborgFactoryComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.ItemsProcessed++;
        if (component.CutOffSound)
        {
            _audio.Stop(component.Stream);
        }

        Dirty(uid, component);
    }

    private bool CanProduce(EntityUid factory,
        XenoborgFactoryComponent component,
        MaterialStorageComponent? storage = null)
    {
        if (!Resolve(factory, ref storage, false))
            return false;
        if (!Proto.TryIndex(component.Recipe, out LatheRecipePrototype? recipe))
            return false;
        foreach (var (material, needed) in recipe.Materials)
        {
            if (_materialStorage.GetMaterialAmount(factory, material) < needed)
                return false;
        }

        foreach (var (material, needed) in recipe.Materials)
        {
            _materialStorage.TryChangeMaterialAmount(factory, material, -needed);
        }

        return true;
    }
}
