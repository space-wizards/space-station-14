using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Transform;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.Transform;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private static readonly Color SelectedOptionBackground = StyleNano.ButtonColorGoodDefault.WithAlpha(128);
    private static readonly Color SelectedOptionHoverBackground = StyleNano.ButtonColorGoodHovered.WithAlpha(128);

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();

        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu == null)
            return;

        if (state is not ChangelingTransformBoundUserInterfaceState current)
            return;

        EntMan.TryGetComponent<ChangelingIdentityComponent>(Owner, out var identityComponent);
        var models = ConvertToButtons(current, identityComponent?.CurrentIdentity);

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
        ChangelingTransformBoundUserInterfaceState current,
        EntityUid? currentIdentity
    )
    {
        var identities = current.Identites;
        var buttons = new List<RadialMenuOptionBase>();
        foreach (var identity in identities)
        {
            var identityUid = EntMan.GetEntity(identity);

            if (!EntMan.TryGetComponent<MetaDataComponent>(identityUid, out var metadata))
                continue;

            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, identity)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identityUid),
                ToolTip = metadata.EntityName,
                BackgroundColor = (currentIdentity == identityUid) ? SelectedOptionBackground : null,
                HoverBackgroundColor = (currentIdentity == identityUid) ? SelectedOptionHoverBackground : null
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
