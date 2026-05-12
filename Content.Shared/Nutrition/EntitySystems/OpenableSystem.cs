using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Provides API for openable food and drinks, handles opening on use and preventing transfer when closed.
/// </summary>
public sealed partial class OpenableSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenableComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OpenableComponent, UseInHandEvent>(OnUse);
        // always try to unlock first before opening
        SubscribeLocalEvent<OpenableComponent, ActivateInWorldEvent>(OnActivated, after: new[] { typeof(LockSystem) });
        SubscribeLocalEvent<OpenableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OpenableComponent, MeleeHitEvent>(HandleIfClosed);
        SubscribeLocalEvent<OpenableComponent, AfterInteractEvent>(HandleIfClosed);
        SubscribeLocalEvent<OpenableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<OpenableComponent, SolutionTransferAttemptEvent>(OnTransferAttempt);
        SubscribeLocalEvent<OpenableComponent, AttemptShakeEvent>(OnAttemptShake);
        SubscribeLocalEvent<OpenableComponent, AttemptAddFizzinessEvent>(OnAttemptAddFizziness);
        SubscribeLocalEvent<OpenableComponent, LockToggleAttemptEvent>(OnLockToggleAttempt);

#if DEBUG
        SubscribeLocalEvent<OpenableComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<OpenableComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Opened && _lock.IsLocked(ent.Owner))
            Log.Error($"Entity {ent} spawned locked open, this is a prototype mistake.");
    }
#else
    }
#endif

    private void OnInit(Entity<OpenableComponent> ent, ref ComponentInit args)
    {
        UpdateAppearance(ent, ent.Comp);
    }

    private void OnUse(Entity<OpenableComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.OpenableByHand)
            return;

        args.Handled = TryOpen(ent, ent, args.User);
    }

    private void OnActivated(Entity<OpenableComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !ent.Comp.OpenOnActivate)
            return;

        args.Handled = TryToggle(ent, args.User);
    }

    private void OnExamined(EntityUid uid, OpenableComponent comp, ExaminedEvent args)
    {
        if (!comp.Opened || !args.IsInDetailsRange)
            return;

        var text = Loc.GetString(comp.ExamineText);
        args.PushMarkup(text);
    }

    private void HandleIfClosed(EntityUid uid, OpenableComponent comp, HandledEntityEventArgs args)
    {
        // prevent spilling/pouring/whatever drinks when closed
        args.Handled = !comp.Opened;
    }

    private void OnGetVerbs(EntityUid uid, OpenableComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Hands == null || !args.CanAccess || !args.CanInteract || _lock.IsLocked(uid))
            return;

        AlternativeVerb verb;
        if (comp.Opened)
        {
            if (!comp.Closeable)
                return;

            verb = new()
            {
                Text = Loc.GetString(comp.CloseVerbText),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
                Act = () => TryClose(args.Target, comp, args.User),
                Priority = 3
            };
        }
        else
        {
            verb = new()
            {
                Text = Loc.GetString(comp.OpenVerbText),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
                Act = () => TryOpen(args.Target, comp, args.User),
                Priority = 3
            };
        }
        args.Verbs.Add(verb);
    }

    private void OnTransferAttempt(Entity<OpenableComponent> ent, ref SolutionTransferAttemptEvent args)
    {
        if (!ent.Comp.Opened)
            args.Cancel(Loc.GetString(ent.Comp.ClosedPopup, ("owner", ent.Owner)));
    }

    private void OnAttemptShake(Entity<OpenableComponent> entity, ref AttemptShakeEvent args)
    {
        // Prevent shaking open containers
        if (entity.Comp.Opened)
            args.Cancelled = true;
    }

    private void OnAttemptAddFizziness(Entity<OpenableComponent> entity, ref AttemptAddFizzinessEvent args)
    {
        // Can't add fizziness to an open container
        if (entity.Comp.Opened)
            args.Cancelled = true;
    }

    private void OnLockToggleAttempt(Entity<OpenableComponent> ent, ref LockToggleAttemptEvent args)
    {
        // can't lock something while it's open
        if (ent.Comp.Opened)
            args.Cancelled = true;
    }

    /// <summary>
    /// Returns true if the entity both has OpenableComponent and is not opened.
    /// Drinks that don't have OpenableComponent are automatically open, so it returns false.
    /// If user is not null a popup will be shown to them.
    /// </summary>
    public bool IsClosed(EntityUid uid, EntityUid? user = null, OpenableComponent? comp = null, bool predicted = false)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (comp.Opened)
            return false;

        if (user != null)
        {
            if (predicted)
                _popup.PopupClient(Loc.GetString(comp.ClosedPopup, ("owner", uid)), user.Value, user.Value);
            else
                _popup.PopupEntity(Loc.GetString(comp.ClosedPopup, ("owner", uid)), user.Value, user.Value);
        }

        return true;
    }

    /// <summary>
    /// Update open visuals to the current value.
    /// </summary>
    public void UpdateAppearance(EntityUid uid, OpenableComponent? comp = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        _appearance.SetData(uid, OpenableVisuals.Opened, comp.Opened, appearance);
    }

    /// <summary>
    /// Sets the opened field and updates open visuals.
    /// </summary>
    public void SetOpen(EntityUid uid, bool opened = true, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || opened == comp.Opened)
            return;

        comp.Opened = opened;
        Dirty(uid, comp);

        if (opened)
        {
            var ev = new OpenableOpenedEvent(user);
            RaiseLocalEvent(uid, ref ev);
        }
        else
        {
            var ev = new OpenableClosedEvent(user);
            RaiseLocalEvent(uid, ref ev);
        }

        UpdateAppearance(uid, comp);
    }

    /// <summary>
    /// If closed, opens it and plays the sound.
    /// </summary>
    /// <returns>Whether it got opened</returns>
    public bool TryOpen(EntityUid uid, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || comp.Opened || _lock.IsLocked(uid))
            return false;

        var ev = new OpenableOpenAttemptEvent(user);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return false;

        SetOpen(uid, true, comp, user);
        _audio.PlayPredicted(comp.Sound, uid, user);
        return true;
    }

    /// <summary>
    /// If opened, closes it and plays the close sound, if one is defined.
    /// </summary>
    /// <returns>Whether it got closed</returns>
    public bool TryClose(EntityUid uid, OpenableComponent? comp = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref comp, false) || !comp.Opened || !comp.Closeable)
            return false;

        SetOpen(uid, false, comp, user);
        if (comp.CloseSound != null)
            _audio.PlayPredicted(comp.CloseSound, uid, user);
        return true;
    }

    /// <summary>
    /// If opened, tries closing it if it's closeable.
    /// If closed, tries opening it.
    /// </summary>
    public bool TryToggle(Entity<OpenableComponent> ent, EntityUid? user)
    {
        if (ent.Comp.Opened && ent.Comp.Closeable)
            return TryClose(ent, ent.Comp, user);

        return TryOpen(ent, ent.Comp, user);
    }
}

/// <summary>
/// Raised after an Openable is opened.
/// </summary>
[ByRefEvent]
public record struct OpenableOpenedEvent(EntityUid? User = null);

/// <summary>
/// Raised after an Openable is closed.
/// </summary>
[ByRefEvent]
public record struct OpenableClosedEvent(EntityUid? User = null);

/// <summary>
/// Raised before trying to open an Openable.
/// </summary>
[ByRefEvent]
public record struct OpenableOpenAttemptEvent(EntityUid? User, bool Cancelled = false);
