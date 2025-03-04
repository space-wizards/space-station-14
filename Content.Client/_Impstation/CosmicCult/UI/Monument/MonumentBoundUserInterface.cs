using Content.Shared._Impstation.Cosmiccult;
using Content.Shared._Impstation.CosmicCult.Components;
using Content.Shared._Impstation.CosmicCult.Prototypes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Impstation.CosmicCult.UI.Monument;
[UsedImplicitly]
public sealed class MonumentBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private MonumentMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MonumentMenu>();

        _menu.OnSelectGlyphButtonPressed += protoId => { SendMessage(new GlyphSelectedMessage(protoId)); };
        _menu.OnRemoveGlyphButtonPressed += () => { SendMessage(new GlyphRemovedMessage()); };

        _menu.OnGainButtonPressed += OnIdSelected;
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not MonumentBuiState buiState)
            return;

        _menu?.UpdateState(buiState);
    }
    private void OnIdSelected(ProtoId<InfluencePrototype> selectedInfluence)
    {
        SendMessage(new InfluenceSelectedMessage(selectedInfluence, EntMan.GetNetEntity(PlayerManager.LocalEntity)));
    }
}
