using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
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
    [Dependency] private readonly SharedLatheSystem _lathe = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedMaterialStorageSystem MaterialStorage = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoborgFactoryComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<XenoborgFactoryComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnCollide(Entity<XenoborgFactoryComponent> entity, ref StartCollideEvent args)
    {
        TryStartProcessItem(entity, args.OtherEntity);
    }

    /// <summary>
    /// Tries to start processing an item via a <see cref="XenoborgFactoryComponent"/>.
    /// </summary>
    protected void TryStartProcessItem(Entity<XenoborgFactoryComponent> factory,
        EntityUid victim,
        EntityUid? user = null,
        bool suicide = false)
    {
        if (_mobState.IsIncapacitated(factory))
            return;

        if (!CanProduce(factory))
            return;

        if (!_mobState.IsIncapacitated(victim) && !HasComp<SleepingComponent>(victim) &&
            _actionBlocker.CanInteract(victim, null) && !suicide)
            return;

        if (_whitelistSystem.IsWhitelistFail(factory.Comp.Whitelist, victim) ||
            _whitelistSystem.IsBlacklistPass(factory.Comp.Blacklist, victim))
            return;

        if (Container.TryGetContainingContainer((victim, null, null), out _) &&
            !Container.TryRemoveFromContainer(victim))
            return;

        if (_timing.CurTime > factory.Comp.NextSound)
        {
            factory.Comp.Stream = _audio.PlayPredicted(factory.Comp.Sound, factory, user)?.Entity;
            factory.Comp.NextSound = _timing.CurTime + factory.Comp.SoundCooldown;
        }

        var reclaimedEvent = new GotReclaimedEvent(Transform(factory).Coordinates);
        RaiseLocalEvent(victim, ref reclaimedEvent);


        Reclaim(factory, victim);
    }

    /// <summary>
    /// Spawns the materials and chemicals associated
    /// with an entity. Also deletes the item.
    /// </summary>
    protected virtual void Reclaim(Entity<XenoborgFactoryComponent> factory, EntityUid victim)
    {
        factory.Comp.ItemsProcessed++;
        if (factory.Comp.CutOffSound)
        {
            _audio.Stop(factory.Comp.Stream);
        }

        Dirty(factory, factory.Comp);
    }

    private bool CanProduce(Entity<XenoborgFactoryComponent, MaterialStorageComponent?> factory)
    {
        if (!Resolve(factory, ref factory.Comp2, false))
            return false;
        if (!Proto.TryIndex(factory.Comp1.Recipe, out var recipe))
            return false;
        foreach (var (material, needed) in recipe.Materials)
        {
            if (MaterialStorage.GetMaterialAmount(factory, material) < needed)
                return false;
        }

        return true;
    }

    private void OnGetVerb(Entity<XenoborgFactoryComponent> factory, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (!Proto.TryIndex(factory.Comp.BorgRecipePack, out var recipePack))
            return;
        var user = args.User;
        foreach (var v in from type in recipePack.Recipes
                 let proto = Proto.Index(type)
                 select new Verb
                 {
                     Category = VerbCategory.SelectType,
                     Text = _lathe.GetRecipeName(proto),
                     Disabled = type == factory.Comp.Recipe,
                     DoContactInteraction = true,
                     Icon = proto.Icon,
                     Act = () =>
                     {
                         factory.Comp.Recipe = type;
                         Popup.PopupPredicted(Loc.GetString("emitter-component-type-set",
                                 ("type", _lathe.GetRecipeName(proto))),
                             factory,
                             user);
                         Dirty(factory, factory.Comp);
                     },
                 })
        {
            args.Verbs.Add(v);
        }
    }
}
