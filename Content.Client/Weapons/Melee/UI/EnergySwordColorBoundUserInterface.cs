using Content.Client.UserInterface.Controls;
using Content.Shared.Weapons.Melee.EnergySword;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Weapons.Melee.UI;

public sealed class EnergySwordColorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private SimpleRadialMenu? _radialMenu;
    private readonly EnergySwordSystem _eswordSystem;

    public EnergySwordColorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _eswordSystem = EntMan.System<EnergySwordSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _radialMenu = this.CreateWindow<SimpleRadialMenu>();

        PopulateRadialMenu();

        _radialMenu.OpenCentered();
    }

    private void PopulateRadialMenu()
    {
        if (_radialMenu == null)
            return;

        if (!EntMan.TryGetComponent<EnergySwordComponent>(Owner, out var esword))
            return;

        if (_player.LocalSession?.AttachedEntity is not { } user)
            return;

        List<RadialMenuOptionBase> list = new();
        foreach (var color in esword.ColorOptions)
        {
            var button = MakeAButton(color);
            list.Add(button);
        }

        var hackedButton = MakeAButton(Color.White, hacked: true);
        list.Add(hackedButton);

        _radialMenu.SetButtons(list);
    }

    private RadialMenuActionOption<Color> MakeAButton(Color color, bool hacked = false)
    {
        EntProtoId<EnergySwordComponent> proto = new("EnergySword"); //Hardcoded bad
        Entity<EnergySwordComponent?> ent = EntMan.Spawn(proto);

        //No comp ?
        ent.Comp ??= EntMan.EnsureComponent<EnergySwordComponent>(ent);


        Entity<EnergySwordComponent> entity = (ent.Owner, ent.Comp);

        //Toggle the esword ON so the blade is out
        _eswordSystem.ActivateSword(entity);
        if (hacked)
            _eswordSystem.ActivateRGB(entity); //RGB when hacked
        else
            _eswordSystem.ChangeColor(entity, color);

        Action<Color> fuctionToCall = hacked ? EnergySwordRGB : PickColor;
        var button = new RadialMenuActionOption<Color>(fuctionToCall, color)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(ent) //The esword sprite with color applied
        };

        return button;
    }

    private void PickColor(Color color)
    {
        SendPredictedMessage(new EnergySwordColorMessage(color));

        Close();
    }

    private void EnergySwordRGB(Color color)
    {
        SendPredictedMessage(new EnergySwordColorMessage(color, rgb: true));
        Close();
    }

}
