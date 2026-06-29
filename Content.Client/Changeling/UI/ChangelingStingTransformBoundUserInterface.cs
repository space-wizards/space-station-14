using JetBrains.Annotations;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.UserInterface;

namespace Content.Client.Changeling.UI;

[UsedImplicitly]
public sealed partial class ChangelingStingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

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

        var models = ConvertToButtons(lingIdentity.ConsumedIdentities);
        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(IEnumerable<ChangelingIdentityData> identities)
    {
        var buttons = new List<RadialMenuOptionBase>();

        foreach (var identity in identities)
        {
            if (identity.Identity == null)
                continue;

            var option = new RadialMenuActionOption<NetEntity>(SendIdentitySelect, EntMan.GetNetEntity(identity.Identity.Value))
            {
                IconSpecifier = RadialMenuIconSpecifier.With(identity.Identity.Value),
                ToolTip = Loc.GetString("changeling-transform-bui-select-entity", ("entity", identity.Identity)),
            };
            buttons.Add(option);
        }

        return buttons;
    }

    private void SendIdentitySelect(NetEntity identityId)
    {
        SendPredictedMessage(new ChangelingStingTransformIdentitySelectMessage(identityId));
    }
}
