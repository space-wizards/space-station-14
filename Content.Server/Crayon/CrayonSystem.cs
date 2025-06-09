using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Decals;
using Content.Server.Popups;
using Content.Shared.Crayon;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Crayon;

public sealed class CrayonSystem : SharedCrayonSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrayonComponent, ComponentInit>(OnCrayonInit);
        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonBoundUI);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonBoundUIColor);
        SubscribeLocalEvent<CrayonComponent, CrayonRotationMessage>(OnCrayonBoundUIRotation);
        SubscribeLocalEvent<CrayonComponent, CrayonPreviewModeMessage>(OnCrayonBoundUIPreviewMode);
        SubscribeLocalEvent<CrayonComponent, BoundUIClosedEvent>(OnBuiClosed);
        SubscribeLocalEvent<CrayonComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<CrayonComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<CrayonComponent, UseInHandEvent>(OnCrayonUse, before: new[] { typeof(FoodSystem) });
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(OnCrayonAfterInteract, after: new[] { typeof(FoodSystem) });
        SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnCrayonDropped);
    }

    private void OnCrayonAfterInteract(EntityUid uid, CrayonComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (component.Charges <= 0)
        {
            if (component.DeleteEmpty)
                UseUpCrayon(uid, args.User);
            else
                _popup.PopupEntity(Loc.GetString("crayon-interact-not-enough-left-text"), uid, args.User);

            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupEntity(Loc.GetString("crayon-interact-invalid-location"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!_decals.TryAddDecal(component.State, args.ClickLocation.Offset(new Vector2(-0.5f, -0.5f)), out _, component.Color, Angle.FromDegrees(component.Rotation), cleanable: true))
            return;

        if (component.UseSound != null)
            _audio.PlayPvs(component.UseSound, uid, AudioParams.Default.WithVariation(0.125f));

        // Decrease "Ammo"
        component.Charges--;
        Dirty(uid, component);

        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{EntityManager.ToPrettyString(args.User):user} drew a {component.Color:color} {component.State}");
        args.Handled = true;

        if (component.DeleteEmpty && component.Charges <= 0)
            UseUpCrayon(uid, args.User);
        else
            _uiSystem.ServerSendUiMessage(uid, CrayonComponent.CrayonUiKey.Key, new CrayonUsedMessage(component.State));
    }

    private void OnCrayonUse(EntityUid uid, CrayonComponent component, UseInHandEvent args)
    {
        // Open crayon window if neccessary.
        if (args.Handled)
            return;

        if (!_uiSystem.HasUi(uid, CrayonComponent.CrayonUiKey.Key))
        {
            return;
        }

        _uiSystem.TryToggleUi(uid, CrayonComponent.CrayonUiKey.Key, args.User);

        _uiSystem.SetUiState(uid, CrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.State, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
        args.Handled = true;
    }

    private void OnCrayonBoundUI(EntityUid uid, CrayonComponent component, CrayonSelectMessage args)
    {
        // Check if the selected state is valid
        if (!_prototypeManager.TryIndex<DecalPrototype>(args.State, out var prototype) || !prototype.Tags.Contains("crayon"))
            return;

        component.State = args.State;

        Dirty(uid, component);
    }

    private void OnCrayonBoundUIColor(EntityUid uid, CrayonComponent component, CrayonColorMessage args)
    {
        // you still need to ensure that the given color is a valid color
        if (!component.SelectableColor || args.Color == component.Color)
            return;

        component.Color = args.Color;
        Dirty(uid, component);

    }

    private void OnCrayonBoundUIRotation(EntityUid uid, CrayonComponent component, CrayonRotationMessage args)
    {
        component.Rotation = args.Rotation;
        Dirty(uid, component);
    }

    private void OnCrayonBoundUIPreviewMode(EntityUid uid, CrayonComponent component, CrayonPreviewModeMessage args)
    {
        if (TryComp<HandsComponent>(args.Actor, out var hands) &&
            TryComp<CrayonComponent>(hands.ActiveHandEntity, out var crayon) &&
            hands.ActiveHandEntity == uid)
        {
            // Only toggle the overlay if the user is holding a crayon in their active hand
            // and check if it is the same crayon that sent the request
            component.PreviewMode = args.PreviewMode;
            Dirty(uid, component);
        }
        else
        {
            // failed to enable, reset button toggle
            _uiSystem.SetUiState(uid, CrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(component.State, component.SelectableColor, component.Color, component.Rotation, component.PreviewMode));
        }
    }

    private void OnBuiClosed(Entity<CrayonComponent> ent, ref BoundUIClosedEvent args)
    {
        DisablePreviewMode(ent);
    }

    private void OnHandDeselected(Entity<CrayonComponent> ent, ref HandDeselectedEvent args)
    {
        DisablePreviewMode(ent);
    }

    private void OnGotUnequipped(Entity<CrayonComponent> ent, ref GotUnequippedEvent args)
    {
        DisablePreviewMode(ent);
    }

    private void DisablePreviewMode(Entity<CrayonComponent> ent)
    {
        ent.Comp.PreviewMode = false;
        Dirty(ent);
        _uiSystem.SetUiState(ent.Owner, CrayonComponent.CrayonUiKey.Key, new CrayonBoundUserInterfaceState(ent.Comp.State, ent.Comp.SelectableColor, ent.Comp.Color, ent.Comp.Rotation, ent.Comp.PreviewMode));
    }

    private void OnCrayonInit(EntityUid uid, CrayonComponent component, ComponentInit args)
    {
        // Get the first one from the catalog and set it as default
        var decal = _prototypeManager.EnumeratePrototypes<DecalPrototype>().FirstOrDefault(x => x.Tags.Contains("crayon"));
        component.State = decal?.ID ?? string.Empty;
        Dirty(uid, component);
    }

    private void OnCrayonDropped(EntityUid uid, CrayonComponent component, DroppedEvent args)
    {
        // TODO: Use the existing event.
        _uiSystem.CloseUi(uid, CrayonComponent.CrayonUiKey.Key, args.User);
    }

    private void UseUpCrayon(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);
        EntityManager.QueueDeleteEntity(uid);
    }
}
