using Content.Shared.Administration.Logs;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Forensics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling.Systems;

public sealed class ChangelingClonerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentity = default!;
    [Dependency] private readonly SharedForensicsSystem _forensics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingClonerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ChangelingClonerComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ChangelingClonerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ChangelingClonerComponent, ClonerDrawDoAfterEvent>(OnDraw);
        SubscribeLocalEvent<ChangelingClonerComponent, ClonerInjectDoAfterEvent>(OnInject);
        SubscribeLocalEvent<ChangelingClonerComponent, ComponentShutdown>(OnShutDown);
    }

    private void OnShutDown(Entity<ChangelingClonerComponent> ent, ref ComponentShutdown args)
    {
        // Delete the stored clone.
        PredictedQueueDel(ent.Comp.ClonedBackup);
    }

    private void OnExamine(Entity<ChangelingClonerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var msg = ent.Comp.State switch
        {
            ChangelingClonerState.Empty => "changeling-cloner-component-empty",
            ChangelingClonerState.Filled => "changeling-cloner-component-filled",
            ChangelingClonerState.Spent => "changeling-cloner-component-spent",
            _ => "error"
        };

        args.PushMarkup(Loc.GetString(msg));

    }

    private void OnGetVerbs(Entity<ChangelingClonerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        if (!ent.Comp.CanReset || ent.Comp.State == ChangelingClonerState.Spent)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("changeling-cloner-component-reset-verb"),
            Disabled = ent.Comp.ClonedBackup == null,
            Act = () => Reset(ent.AsNullable(), user),
            DoContactInteraction = true,
        });
    }

    private void OnAfterInteract(Entity<ChangelingClonerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        switch (ent.Comp.State)
        {
            case ChangelingClonerState.Empty:
                args.Handled |= TryDraw(ent.AsNullable(), args.Target.Value, args.User);
                break;
            case ChangelingClonerState.Filled:
                args.Handled |= TryInject(ent.AsNullable(), args.Target.Value, args.User);
                break;
            case ChangelingClonerState.Spent:
            default:
                break;
        }

    }

    private void OnDraw(Entity<ChangelingClonerComponent> ent, ref ClonerDrawDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        Draw(ent.AsNullable(), args.Target.Value, args.User);
        args.Handled = true;
    }

    private void OnInject(Entity<ChangelingClonerComponent> ent, ref ClonerInjectDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        Inject(ent.AsNullable(), args.Target.Value, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Start a DoAfter to draw a DNA sample from the target.
    /// </summary>
    public bool TryDraw(Entity<ChangelingClonerComponent?> ent, EntityUid target, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State != ChangelingClonerState.Empty)
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false; // cloning only works for humanoids at the moment

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfter, new ClonerDrawDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        var userIdentity = Identity.Entity(user, EntityManager);
        var targetIdentity = Identity.Entity(target, EntityManager);
        var userMsg = Loc.GetString("changeling-cloner-component-draw-user", ("user", userIdentity), ("target", targetIdentity));
        var targetMsg = Loc.GetString("changeling-cloner-component-draw-target", ("user", userIdentity), ("target", targetIdentity));
        _popup.PopupClient(userMsg, target, user);

        if (user != target) // don't show the warning if using the item on yourself
            _popup.PopupEntity(targetMsg, user, target, PopupType.LargeCaution);

        return true;
    }

    /// <summary>
    /// Start a DoAfter to inject a DNA sample into someone, turning them into a clone of the original.
    /// </summary>
    public bool TryInject(Entity<ChangelingClonerComponent?> ent, EntityUid target, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State != ChangelingClonerState.Filled)
            return false;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return false; // cloning only works for humanoids at the moment

        var args = new DoAfterArgs(EntityManager, user, ent.Comp.DoAfter, new ClonerInjectDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return false;

        var userIdentity = Identity.Entity(user, EntityManager);
        var targetIdentity = Identity.Entity(target, EntityManager);
        var userMsg = Loc.GetString("changeling-cloner-component-inject-user", ("user", userIdentity), ("target", targetIdentity));
        var targetMsg = Loc.GetString("changeling-cloner-component-inject-target", ("user", userIdentity), ("target", targetIdentity));
        _popup.PopupClient(userMsg, target, user);

        if (user != target) // don't show the warning if using the item on yourself
            _popup.PopupEntity(targetMsg, user, target, PopupType.LargeCaution);

        return true;
    }

    /// <summary>
    /// Draw a DNA sample from the target.
    /// This will create a clone stored on a paused map for data storage.
    /// </summary>
    public void Draw(Entity<ChangelingClonerComponent?> ent, EntityUid target, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.State != ChangelingClonerState.Empty)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return; // cloning only works for humanoids at the moment

        if (!_prototype.Resolve(ent.Comp.Settings, out var settings))
            return;

        _adminLogger.Add(LogType.Identity,
            $"{user} is using {ent.Owner} to draw DNA from {target}.");

        // Make a copy of the target on a paused map, so that we can apply their components later.
        ent.Comp.ClonedBackup = _changelingIdentity.CloneToPausedMap(settings, target);
        ent.Comp.State = ChangelingClonerState.Filled;
        _appearance.SetData(ent.Owner, ChangelingClonerVisuals.State, ChangelingClonerState.Filled);
        Dirty(ent);

        _audio.PlayPredicted(ent.Comp.DrawSound, target, user);
        _forensics.TransferDna(ent, target);
    }

    /// <summary>
    /// Inject a DNA sample into someone, turning them into a clone of the original.
    /// </summary>
    public void Inject(Entity<ChangelingClonerComponent?> ent, EntityUid target, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.State != ChangelingClonerState.Filled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return; // cloning only works for humanoids at the moment

        if (!_prototype.Resolve(ent.Comp.Settings, out var settings))
            return;

        _audio.PlayPredicted(ent.Comp.InjectSound, target, user);
        _forensics.TransferDna(ent, target); // transfer DNA before overwriting it

        if (!ent.Comp.Reusable)
        {
            ent.Comp.State = ChangelingClonerState.Spent;
            _appearance.SetData(ent.Owner, ChangelingClonerVisuals.State, ChangelingClonerState.Spent);
            Dirty(ent);
        }

        if (!Exists(ent.Comp.ClonedBackup))
            return; // the entity is likely out of PVS range on the client

        _adminLogger.Add(LogType.Identity,
            $"{user} is using {ent.Owner} to inject DNA into {target} changing their identity to {ent.Comp.ClonedBackup.Value}.");

        // Do the actual transformation.
        _humanoidAppearance.CloneAppearance(ent.Comp.ClonedBackup.Value, target);
        _cloning.CloneComponents(ent.Comp.ClonedBackup.Value, target, settings);
        _metaData.SetEntityName(target, Name(ent.Comp.ClonedBackup.Value), raiseEvents: ent.Comp.RaiseNameChangeEvents);

    }

    /// <summary>
    /// Purge the stored DNA and allow to draw again.
    /// </summary>
    public void Reset(Entity<ChangelingClonerComponent?> ent, EntityUid? user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // Delete the stored clone.
        PredictedQueueDel(ent.Comp.ClonedBackup);
        ent.Comp.ClonedBackup = null;
        ent.Comp.State = ChangelingClonerState.Empty;
        _appearance.SetData(ent.Owner, ChangelingClonerVisuals.State, ChangelingClonerState.Empty);
        Dirty(ent);

        if (user == null)
            return;

        _popup.PopupClient(Loc.GetString("changeling-cloner-component-reset-popup"), user.Value, user.Value);
    }
}

/// <summary>
/// Doafter event for drawing a DNA sample.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClonerDrawDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// DoAfterEvent for injecting a DNA sample, turning a player into someone else.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ClonerInjectDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Key for the generic visualizer.
/// </summary>
[Serializable, NetSerializable]
public enum ChangelingClonerVisuals : byte
{
    State,
}
