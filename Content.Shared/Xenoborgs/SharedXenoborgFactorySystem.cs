using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
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
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem MaterialStorage = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CollideXenoborgFactoryComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<XenoborgFactoryComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnCollide(EntityUid uid, CollideXenoborgFactoryComponent component, ref StartCollideEvent args)
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
    protected void TryStartProcessItem(EntityUid uid,
        EntityUid item,
        XenoborgFactoryComponent? component = null,
        EntityUid? user = null,
        bool suicide = false)
    {
        if (!Resolve(uid, ref component))
            return;

        if (_mobState.IsIncapacitated(uid))
            return;

        if (!CanProduce(uid, component))
            return;

        if (!_mobState.IsIncapacitated(item) && !HasComp<SleepingComponent>(item) &&
            _actionBlocker.CanInteract(item, null) && !suicide)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, item) ||
            _whitelistSystem.IsBlacklistPass(component.Blacklist, item))
            return;

        if (Container.TryGetContainingContainer((item, null, null), out _) && !Container.TryRemoveFromContainer(item))
            return;

        if (_timing.CurTime > component.NextSound)
        {
            component.Stream = _audio.PlayPredicted(component.Sound, uid, user)?.Entity;
            component.NextSound = _timing.CurTime + component.SoundCooldown;
        }

        var reclaimedEvent = new GotReclaimedEvent(Transform(uid).Coordinates);
        RaiseLocalEvent(item, ref reclaimedEvent);


        Reclaim(uid, item, component);
    }

    /// <summary>
    /// Spawns the materials and chemicals associated
    /// with an entity. Also deletes the item.
    /// </summary>
    protected virtual void Reclaim(EntityUid uid, EntityUid item, XenoborgFactoryComponent? component = null)
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
        if (!Proto.TryIndex(component.Recipe, out var recipe))
            return false;
        foreach (var (material, needed) in recipe.Materials)
        {
            if (MaterialStorage.GetMaterialAmount(factory, material) < needed)
                return false;
        }

        return true;
    }

    protected abstract void OnGetVerb(EntityUid uid, XenoborgFactoryComponent component, GetVerbsEvent<Verb> args);
}
