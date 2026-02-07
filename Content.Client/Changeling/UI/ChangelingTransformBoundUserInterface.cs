using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.UI;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private static readonly Color SelectedOptionBackground = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        Update();
        _menu.OpenOverMouseScreenPosition();
    }


    public override void Update()
    {
        if (_menu == null)
            return;

        if (!EntMan.TryGetComponent<ChangelingIdentityComponent>(Owner, out var lingIdentity))
            return;

        var models = ConvertToButtons(lingIdentity.ConsumedIdentities, lingIdentity?.CurrentIdentity);

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
        IEnumerable<EntityUid> identities,
        EntityUid? currentIdentity
    )
    {
        var buttons = new List<RadialMenuOptionBase>();
        foreach (var identity in identities)
        {
            if (!EntMan.TryGetComponent<MetaDataComponent>(identity, out var metadata))
                continue;

            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, EntMan.GetNetEntity(identity))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity),
                ToolTip = metadata.EntityName,
                BackgroundColor = (currentIdentity == identity) ? SelectedOptionBackground : null,
                HoverBackgroundColor = (currentIdentity == identity) ? SelectedOptionHoverBackground : null
            };
            buttons.Add(option);
        }

        return buttons;
    }

    private void SendIdentitySelect(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingTransformIdentitySelectMessage(identityId));
    }
}
