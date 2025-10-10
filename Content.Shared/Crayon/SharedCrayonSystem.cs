using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Decals;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using System.Numerics;
using System.Linq;

namespace Content.Shared.Crayon;

public abstract partial class SharedCrayonSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDecalSystem _decal = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string _crayonTag = "crayon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrayonComponent, UseInHandEvent>(OnUseInHand, before: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<CrayonComponent, AfterInteractEvent>(OnAfterInteractEntity, after: new[] { typeof(IngestionSystem) });
        SubscribeLocalEvent<CrayonComponent, DroppedEvent>(OnDropped);

        SubscribeLocalEvent<CrayonComponent, CrayonSelectMessage>(OnCrayonSelect);
        SubscribeLocalEvent<CrayonComponent, CrayonColorMessage>(OnCrayonColor);
    }

    private void OnUseInHand(Entity<CrayonComponent> ent, ref UseInHandEvent args)
    {
        // Open crayon window if neccessary.
        if (args.Handled)
            return;

        if (!_ui.HasUi(ent.Owner, CrayonUiKey.Key))
            return;

        _ui.SetUiState(ent.Owner, CrayonUiKey.Key, new CrayonBoundUserInterfaceState(ent.Comp.SelectedState, ent.Comp.SelectableColor, ent.Comp.Color));
        _ui.TryToggleUi(ent.Owner, CrayonUiKey.Key, args.User);

        args.Handled = true;
    }

    private void OnAfterInteractEntity(Entity<CrayonComponent> ent, ref AfterInteractEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled || !args.CanReach)
            return;

        if (ent.Comp.Charges <= 0)
        {
            if (ent.Comp.DeleteEmpty)
                UseUpCrayon(ent.Owner, args.User);
            else
                _popup.PopupPredicted(Loc.GetString("crayon-interact-not-enough-left-text"), ent.Owner, args.User);

            args.Handled = true;
            return;
        }

        if (!args.ClickLocation.IsValid(EntityManager))
        {
            _popup.PopupPredicted(Loc.GetString("crayon-interact-invalid-location"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        if (!_decal.TryAddDecal(new Decal(ent.Comp.SelectedState).WithColor(ent.Comp.Color), args.ClickLocation.Offset(new Vector2(-0.5f, -0.5f)), out var _))
            return;

        if (ent.Comp.UseSound != null)
            _audio.PlayPredicted(ent.Comp.UseSound, ent.Owner, null, AudioParams.Default.WithVariation(0.125f));

        // Decrease "Ammo"
        ent.Comp.Charges--;
        Dirty(ent);

        _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low, $"{ToPrettyString(args.User):user} drew a {ent.Comp.Color:color} {ent.Comp.SelectedState}");
        args.Handled = true;

        if (ent.Comp.DeleteEmpty && ent.Comp.Charges <= 0)
            UseUpCrayon(ent.Owner, args.User);
        else if (_ui.TryGetOpenUi(ent.Owner, CrayonUiKey.Key, out var bui))
            _ui.SendPredictedUiMessage(bui, new CrayonUsedMessage(ent.Comp.SelectedState));
    }

    private void OnDropped(Entity<CrayonComponent> ent, ref DroppedEvent args)
    {
        _ui.CloseUi(ent.Owner, CrayonUiKey.Key, args.User);
    }

    private void OnCrayonSelect(Entity<CrayonComponent> ent, ref CrayonSelectMessage args)
    {
        if (!_prototypeManager.Resolve<DecalPrototype>(args.State, out var prototype))
            return;

        // Check if the selected state is valid
        if (!prototype.Tags.Contains(_crayonTag))
            return;

        ent.Comp.SelectedState = args.State;
        Dirty(ent);
    }

    private void OnCrayonColor(Entity<CrayonComponent> ent, ref CrayonColorMessage args)
    {
        if (!ent.Comp.SelectableColor)
            return;

        // you still need to ensure that the given color is a valid color
        if (ent.Comp.Color != args.Color)
            return;

        ent.Comp.Color = args.Color;
        Dirty(ent);
    }

    private void UseUpCrayon(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("crayon-interact-used-up-text", ("owner", uid)), user, user);

        PredictedDel(uid);
        /*PredictedQueueDel(uid);*/
   }
}
