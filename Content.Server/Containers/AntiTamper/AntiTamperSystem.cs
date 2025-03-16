using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Containers.AntiTamper;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Lock;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Containers.AntiTamper;

public sealed partial class AntiTamperSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly LockSystem _lockSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntiTamperComponent, DestructionEventArgs>(OnDestroy, before: [typeof(EntityStorageSystem)]);
        SubscribeLocalEvent<AntiTamperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AntiTamperComponent, AntiTamperDisarmDoAfterEvent>(OnDisarmDoAfter);
    }

    private void OnDestroy(EntityUid uid, AntiTamperComponent comp, DestructionEventArgs args)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        if (!_lockSystem.IsLocked(uid))
            return;

        var coords = Transform(uid).Coordinates;

        if (_random.Prob(0.25f))
        {
            _popupSystem.PopupCoordinates(Loc.GetString(comp.FailureMessage, ("container", uid)), coords, PopupType.Small);
            return;
        }

        foreach (var container in _containerSystem.GetAllContainers(uid, containerManager))
        {
            if (comp.Containers != null && !comp.Containers.Contains(container.ID))
                continue;

            foreach (var ent in container.ContainedEntities)
            {
                if (comp.PreventRoundRemoval && HasComp<MindContainerComponent>(ent))
                {
                    _damageableSystem.TryChangeDamage(ent, comp.MobDamage);
                }
                else
                {
                    QueueDel(ent);
                }
            }
        }

        _popupSystem.PopupCoordinates(Loc.GetString(comp.Message, ("container", uid)), coords, PopupType.SmallCaution);
        _audioSystem.PlayPvs(comp.Sound, coords);
    }

    private void OnGetVerbs(EntityUid uid, AntiTamperComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (!CanDisarm((uid, comp), args.User, args.Using))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("anti-tamper-disarm-verb"),
            Act = () =>
            {
                AttemptDisarm((uid, comp), args.User, args.Using);
            }
        };
        args.Verbs.Add(verb);
    }

    private bool CanDisarm(Entity<AntiTamperComponent> ent, EntityUid user, EntityUid? used)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (comp.DisarmToolRequired != null && (used == null || !_toolSystem.HasQuality(used.Value, comp.DisarmToolRequired)))
            return false;

        // Check if crate is unlocked+open or if the player is inside
        if (!_containerSystem.ContainsEntity(uid, user))
        {
            // Disarming entity is outside of the crate
            // Crate must be unlocked+open

            if (!TryComp<EntityStorageComponent>(uid, out var entStorage))
                return false;
            if (_lockSystem.IsLocked(uid) || !entStorage.Open)
                return false;
        }

        return true;
    }

    private void AttemptDisarm(Entity<AntiTamperComponent> ent, EntityUid user, EntityUid? used)
    {
        if (!CanDisarm(ent, user, used))
            return;

        var delay = ent.Comp.DisarmTime;
        if (_lockSystem.IsLocked(ent.Owner))
            delay *= ent.Comp.DisarmLockedMultiplier;

        if (ent.Comp.DisarmToolRequired != null)
            _toolSystem.UseTool(
                used!.Value,
                user,
                ent.Owner,
                delay,
                [ent.Comp.DisarmToolRequired.Value],
                new AntiTamperDisarmDoAfterEvent(),
                out _
            );
        else
            _doAfterSystem.TryStartDoAfter(
                new DoAfterArgs(EntityManager, user, delay, new AntiTamperDisarmDoAfterEvent(), ent, ent, used)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    BreakOnDropItem = true,
                    MovementThreshold = 1.0f,
                    BlockDuplicate = true,
                    DuplicateCondition = DuplicateConditions.SameTarget | DuplicateConditions.SameEvent,
                }
            );
    }

    private void OnDisarmDoAfter(EntityUid uid, AntiTamperComponent comp, AntiTamperDisarmDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null || args.Args.Used == null)
            return;

        RemComp<AntiTamperComponent>(uid);
        _popupSystem.PopupEntity(Loc.GetString("anti-tamper-disarmed", ("container", uid)), uid, args.User);
    }
}
