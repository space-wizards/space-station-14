using Content.Shared.Electrocution;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Electrocution;

/// <summary>
/// Shows the Electrocution HUD to entities with the ShowElectrocutionHUDComponent.
/// </summary>
public sealed class ElectrocutionHUDVisualizerSystem : VisualizerSystem<ElectrocutionHUDVisualsComponent>
{
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowElectrocutionHUDComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShowElectrocutionHUDComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShowElectrocutionHUDComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ShowElectrocutionHUDComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(Entity<ShowElectrocutionHUDComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ShowHUD();
    }

    private void OnPlayerDetached(Entity<ShowElectrocutionHUDComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveHUD();
    }

    private void OnInit(Entity<ShowElectrocutionHUDComponent> ent, ref ComponentInit args)
    {
        if (_playerMan.LocalEntity == ent)
        {
            ShowHUD();
        }
    }

    private void OnShutdown(Entity<ShowElectrocutionHUDComponent> ent, ref ComponentShutdown args)
    {
        if (_playerMan.LocalEntity == ent)
        {
            RemoveHUD();
        }
    }

    // Show the HUD to the client.
    // We have to look for all current entities that can be electrified and toggle the HUD layer on if they are.
    private void ShowHUD()
    {
        var electrifiedQuery = AllEntityQuery<ElectrocutionHUDVisualsComponent, AppearanceComponent, SpriteComponent>();
        while (electrifiedQuery.MoveNext(out var uid, out _, out var appearanceComp, out var spriteComp))
        {
            if (!AppearanceSystem.TryGetData<bool>(uid, ElectrifiedVisuals.IsElectrified, out var electrified, appearanceComp))
                continue;

            _sprite.LayerSetVisible((uid, spriteComp), ElectrifiedLayers.HUD, electrified);
        }
    }

    // Remove the HUD from the client.
    // Find all current entities that can be electrified and hide the HUD layer.
    private void RemoveHUD()
    {
        var electrifiedQuery = AllEntityQuery<ElectrocutionHUDVisualsComponent, AppearanceComponent, SpriteComponent>();
        while (electrifiedQuery.MoveNext(out var uid, out _, out _, out var spriteComp))
        {
            _sprite.LayerSetVisible((uid, spriteComp), ElectrifiedLayers.HUD, false);
        }
    }

    // Toggle the HUD layer if an entity becomes (de-)electrified
    protected override void OnAppearanceChange(EntityUid uid, ElectrocutionHUDVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, ElectrifiedVisuals.IsElectrified, out var electrified, args.Component))
            return;

        var player = _playerMan.LocalEntity;
        _sprite.LayerSetVisible((uid, args.Sprite), ElectrifiedLayers.HUD, electrified && HasComp<ShowElectrocutionHUDComponent>(player));
    }
}
