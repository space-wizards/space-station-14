// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT
// Official port from the BACKMEN project. Make sure to review the original repository to avoid license violations.

using Content.Shared.Backmen.FootPrint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.Backmen.FootPrint;

public sealed class FootPrintsVisualizerSystem : VisualizerSystem<FootPrintComponent>
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FootPrintComponent, ComponentInit>(OnInitialized);
        SubscribeLocalEvent<FootPrintComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FootPrintComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<FootPrintComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not FootPrintState state || !TryGetEntity(state.PrintOwner, out var entity))
            return;

        ent.Comp.PrintOwner = entity.Value;
    }

    private void OnInitialized(EntityUid uid, FootPrintComponent comp, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapReserveBlank(FootPrintVisualLayers.Print);
        UpdateAppearance(uid, comp, sprite);
    }

    private void OnShutdown(EntityUid uid, FootPrintComponent comp, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(FootPrintVisualLayers.Print, out var layer))
        {
            sprite.RemoveLayer(layer);
        }
    }

    private void UpdateAppearance(EntityUid uid, FootPrintComponent component, SpriteComponent sprite)
    {
        if (!sprite.LayerMapTryGet(FootPrintVisualLayers.Print, out var layer))
            return;

        if (!TryComp<FootPrintsComponent>(component.PrintOwner, out var printsComponent))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        if (_appearance.TryGetData<FootPrintVisuals>(uid, FootPrintVisualState.State, out var printVisuals, appearance))
        {
            switch (printVisuals)
            {
                case FootPrintVisuals.BareFootPrint:
                    sprite.LayerSetState(layer,
                            printsComponent.RightStep
                            ? new RSI.StateId(printsComponent.RightBarePrint)
                            : new RSI.StateId(printsComponent.LeftBarePrint),
                        printsComponent.RsiPath);
                    break;
                case FootPrintVisuals.ShoesPrint:
                    sprite.LayerSetState(layer, new RSI.StateId(printsComponent.ShoesPrint), printsComponent.RsiPath);
                    break;
                case FootPrintVisuals.SuitPrint:
                    sprite.LayerSetState(layer, new RSI.StateId(printsComponent.SuitPrint), printsComponent.RsiPath);
                    break;
                case FootPrintVisuals.Dragging:
                    sprite.LayerSetState(layer, new RSI.StateId(_random.Pick(printsComponent.DraggingPrint)), printsComponent.RsiPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown {printVisuals} parameter.");
            }
        }

        if (!_appearance.TryGetData<Color>(uid, FootPrintVisualState.Color, out var printColor, appearance))
            return;

        sprite.LayerSetColor(layer, printColor);
    }

    protected override void OnAppearanceChange (EntityUid uid, FootPrintComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        UpdateAppearance(uid, component, sprite);
    }
}
