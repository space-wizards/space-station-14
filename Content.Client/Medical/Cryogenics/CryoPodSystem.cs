using System.Numerics;
using Content.Shared.Chemistry;
using Content.Shared.Emag.Systems;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using static Content.Shared.Medical.Cryogenics.CryoPodComponent;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.Medical.Cryogenics;

public sealed class CryoPodSystem: SharedCryoPodSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryoPodComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryoPodComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CryoPodComponent, CryoPodPryFinished>(OnCryoPodPryFinished);
        SubscribeLocalEvent<CryoPodComponent, AppearanceChangeEvent>(OnAppearanceChange);

        SubscribeLocalEvent<InsideCryoPodComponent, ComponentStartup>(OnCryoPodInsertion);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentRemove>(OnCryoPodRemoval);
    }

    private void OnCryoPodInsertion(EntityUid uid, InsideCryoPodComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        component.PreviousOffset = spriteComponent.Offset;
        spriteComponent.Offset = new Vector2(0, 1);
    }

    private void OnCryoPodRemoval(EntityUid uid, InsideCryoPodComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
        {
            return;
        }

        spriteComponent.Offset = component.PreviousOffset;
    }

    private void OnAppearanceChange(EntityUid uid, CryoPodComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
        {
            return;
        }

        if (!_appearance.TryGetData<bool>(uid, CryoPodVisuals.ContainsEntity, out var isOpen, args.Component) ||
            !_appearance.TryGetData<bool>(uid, CryoPodVisuals.IsOn, out var isOn, args.Component) ||
            !_appearance.TryGetData<Color>(uid, SolutionContainerVisuals.Color, out var color, args.Component))
        {
            return;
        }

        if (isOpen) // Cryo open, no one inside
        {
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, "pod-open");
            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, false);
            args.Sprite.DrawDepth = (int)DrawDepth.Objects;
            if (_pointLightSystem.TryGetLight(uid, out var pointLight))
            {
                _pointLightSystem.SetEnabled(uid, false, pointLight);
            }
        }
        else
        {
            args.Sprite.DrawDepth = (int)DrawDepth.Mobs;
            args.Sprite.LayerSetState(CryoPodVisualLayers.Base, "pod-off");
            args.Sprite.LayerSetState(CryoPodVisualLayers.Cover, isOn ? "cover-on" : "cover-off");

            args.Sprite.LayerSetColor(CryoPodVisualLayers.Cover, color);

            args.Sprite.LayerSetVisible(CryoPodVisualLayers.Cover, true);


            if (_pointLightSystem.TryGetLight(uid, out var pointLight))
            {
                if (!_appearance.TryGetData<float>(uid, SolutionContainerVisuals.FillFraction, out var fraction, args.Component) || fraction == 0)
                {
                    _pointLightSystem.SetEnabled(uid, false, pointLight);
                }
                else
                {
                    _pointLightSystem.SetEnabled(uid, true, pointLight);
                    _pointLightSystem.SetColor(uid, color, pointLight);
                }
            }
        }
    }
}

public enum CryoPodVisualLayers : byte
{
    Base,
    Cover,
}
