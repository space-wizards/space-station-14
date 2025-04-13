using Content.Client.UserInterface.Controls;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Ranged.UI;

public sealed class BatteryWeaponFireModesBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private SimpleRadialMenu? _menu;

    private readonly Color _selectedModeBackgroundColor = Color.ToSrgb(new Color(90, 73, 102, 128));
    private readonly Color _selectedModeHoverBackgroundColor = Color.ToSrgb(new Color(107, 91, 127, 128));

    /// <inheritdoc />
    public BatteryWeaponFireModesBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent(Owner, out BatteryWeaponFireModesComponent? fireModes))
            return;
        

        _menu = this.CreateWindow<SimpleRadialMenu>();
        var models = CreateButtons(fireModes);
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOption> CreateButtons(BatteryWeaponFireModesComponent fireModes)
    {
        var list = new List<RadialMenuActionOption>();

        for (var i = 0; i < fireModes.FireModes.Count; i++)
        {
            var fireMode = fireModes.FireModes[i];
            var entProto = _prototypeManager.Index<EntityPrototype>(fireMode.Prototype);
            var index = i;

            var option = new RadialMenuActionOption<BatteryWeaponFireMode>(mode => HandleRadialMenuClick(index), fireMode)
            {
                ToolTip = entProto.Name,
                Sprite = fireMode.ModeIcon
            };
            if (index == fireModes.CurrentFireMode)
            {
                option.BackgroundColor = _selectedModeBackgroundColor;
                option.HoverBackgroundColor = _selectedModeHoverBackgroundColor;
            }

            list.Add(option);
        }

        return list;
    }

    private void HandleRadialMenuClick(int modeIndex)
    {
        var msg = new BatteryWeaponFireModeChangeMessage { ModeIndex = modeIndex };
        SendPredictedMessage(msg);
    }
}
