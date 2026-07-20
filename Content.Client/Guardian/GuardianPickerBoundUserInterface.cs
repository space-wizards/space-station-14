using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.Guardian;
using Content.Shared.Guardian.Components;
using JetBrains.Annotations;
using Robust.Client.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Guardian;

[UsedImplicitly]
public sealed partial class GuardianPickerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;
    private static readonly Color SelectedOptionHoverBackground = Palettes.Green.HoveredElement.WithAlpha(128);

    [Dependency] private ClientPrototypeManager _prototypes = default!;
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

        if (!EntMan.TryGetComponent<GuardianCreatorComponent>(Owner, out var creatorEntity))
            return;

        var models = ConvertToButtons(creatorEntity.Guardians);

        _menu.SetButtons(models);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(
        IEnumerable<ProtoId<GuardianEntryPrototype>> guardians
    )
    {
        var buttons = new List<RadialMenuOptionBase>();
        var dropButtons = new List<RadialMenuOptionBase>();
        var index = 0u;

        foreach (var guardian in guardians)
        {
            index += 1;
            if (!_prototypes.Resolve(guardian, out var proto))
                continue;

            // Options for selecting guardians
            var option = new RadialMenuActionOption<uint>(SendGuardianSelect, index - 1)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(proto.Icon),
                ToolTip = Loc.GetString(proto.Description),
                HoverBackgroundColor = SelectedOptionHoverBackground
            };
            buttons.Add(option);
        }

        return buttons;
    }

    private void SendGuardianSelect(uint index)
    {
        SendPredictedMessage(new GuardianPicked(index));
    }
}
