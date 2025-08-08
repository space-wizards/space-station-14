using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Transform;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.Transform;

[UsedImplicitly]
public sealed partial class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();

        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (_menu == null)
            return;

        base.UpdateState(state);

        if (state is not ChangelingTransformBoundUserInterfaceState current)
            return;

        var models = ConvertToButtons(current);

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(ChangelingTransformBoundUserInterfaceState current)
    {
        var buttons = new List<RadialMenuOptionBase>();
        foreach (var identity in current.Identites)
        {
            var identityUid = EntMan.GetEntity(identity);

            if (!EntMan.TryGetComponent<MetaDataComponent>(identityUid, out var metadata))
                continue;

            var identityName = metadata.EntityName;

            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, identity)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identityUid),
                ToolTip = identityName
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
