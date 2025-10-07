using Content.Client.UserInterface.Systems.Sandbox;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.SubFloor;

public sealed class SubFloorHideSystem : SharedSubFloorHideSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private bool _showAll;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ShowAll
    {
        get => _showAll;
        set
        {
            if (_showAll == value) return;
            _showAll = value;
            _ui.GetUIController<SandboxUIController>().SetToggleSubfloors(value);

            var ev = new ShowSubfloorRequestEvent()
            {
                Value = value,
            };
            RaiseNetworkEvent(ev);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubFloorHideComponent, AppearanceChangeEvent>(OnAppearanceChanged);
        SubscribeNetworkEvent<ShowSubfloorRequestEvent>(OnRequestReceived);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent ev)
    {
        // Vismask resets so need to reset this.
        ShowAll = false;
    }

    private void OnRequestReceived(ShowSubfloorRequestEvent ev)
    {
        // When client receives request Queue an update on all vis.
        UpdateAll();
    }

    private void OnAppearanceChanged(EntityUid uid, SubFloorHideComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _appearance.TryGetData<bool>(uid, SubFloorVisuals.Covered, out var covered, args.Component);
        _appearance.TryGetData<bool>(uid, SubFloorVisuals.ScannerRevealed, out var scannerRevealed, args.Component);

        scannerRevealed &= !ShowAll; // no transparency for show-subfloor mode.

        var revealed = !covered || ShowAll || scannerRevealed;

        // set visibility & color of each layer
        foreach (var layer in args.Sprite.AllLayers)
        {
            // pipe connection visuals are updated AFTER this, and may re-hide some layers
            layer.Visible = revealed;
        }

        // Is there some layer that is always visible?
        var hasVisibleLayer = false;
        foreach (var layerKey in component.VisibleLayers)
        {
            if (!_sprite.LayerMapTryGet((uid, args.Sprite), layerKey, out var layerIndex, false))
                continue;

            var layer = args.Sprite[layerIndex];
            layer.Visible = true;
            layer.Color = layer.Color.WithAlpha(1f);
            hasVisibleLayer = true;
        }

        _sprite.SetVisible((uid, args.Sprite), hasVisibleLayer || revealed);

        if (ShowAll)
        {
            // Allows sandbox mode to make wires visible over other stuff.
            component.OriginalDrawDepth ??= args.Sprite.DrawDepth;
            _sprite.SetDrawDepth((uid, args.Sprite), (int)Shared.DrawDepth.DrawDepth.Overdoors);
        }
        else if (scannerRevealed)
        {
            // Allows a t-ray to show wires/pipes above carpets/puddles.
            if (component.OriginalDrawDepth is not null)
                return;
            component.OriginalDrawDepth = args.Sprite.DrawDepth;
            var drawDepthDifference = Shared.DrawDepth.DrawDepth.ThickPipe - Shared.DrawDepth.DrawDepth.Puddles;
            _sprite.SetDrawDepth((uid, args.Sprite), args.Sprite.DrawDepth - (drawDepthDifference - 1));
        }
        else if (component.OriginalDrawDepth.HasValue)
        {
            _sprite.SetDrawDepth((uid, args.Sprite), component.OriginalDrawDepth.Value);
            component.OriginalDrawDepth = null;
        }
    }

    private void UpdateAll()
    {
        var query = AllEntityQuery<SubFloorHideComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance))
        {
            _appearance.QueueUpdate(uid, appearance);
        }
    }
}
