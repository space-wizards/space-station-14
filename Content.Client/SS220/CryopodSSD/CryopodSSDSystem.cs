// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CryopodSSD;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.CryopodSSD;

public sealed class CryopodSSDSystem : SharedCryopodSSDSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CryopodSSDComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CryopodSSDComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryopodSSDComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, CryopodSSDComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is null)
        {
            return;
        }
        
        if (!_appearanceSystem.TryGetData<bool>(uid, CryopodSSDComponent.CryopodSSDVisuals.ContainsEntity, out var isOpen, args.Component))
        {
            return;
        }

        args.Sprite.LayerSetState(CryopodSSDVisualLayers.Cover, isOpen ? "cryopodSSD-open" : "cryopodSSD-closed");
    }
}

public enum CryopodSSDVisualLayers : byte
{
    Cover
}