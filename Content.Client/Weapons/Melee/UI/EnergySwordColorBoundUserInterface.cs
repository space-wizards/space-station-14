using Content.Client.UserInterface.Controls;
using Content.Shared.Weapons.Melee.EnergySword;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Weapons.Melee.UI;

public sealed class EnergySwordColorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private SimpleRadialMenu? _radialMenu;
    private readonly EnergySwordSystem _eswordSystem;
    private List<Entity<EnergySwordComponent>> _colorEswords = new();
    private Entity<EnergySwordComponent> _rgbEsword;

    public EnergySwordColorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _eswordSystem = EntMan.System<EnergySwordSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<EnergySwordComponent>(Owner, out var esword))
            return;

        if (_player.LocalSession?.AttachedEntity is not { } user)
            return;

        foreach (var color in esword.ColorOptions)
        {
            _colorEswords.Add(SpawnEsword(color));
        }

        _rgbEsword = SpawnRgbEsword();

        _radialMenu = this.CreateWindow<SimpleRadialMenu>();

        PopulateRadialMenu();

        _radialMenu.OpenCentered();
    }

    private void PopulateRadialMenu()
    {
        if (_radialMenu == null)
            return;

        List<RadialMenuOptionBase> list = new();

        foreach (var eswordEnt in _colorEswords)
        {
            var button = new RadialMenuActionOption<Color>(PickColor, eswordEnt.Comp.ActivatedColor)
            {
                //The esword sprite with color applied
                IconSpecifier = RadialMenuIconSpecifier.With(eswordEnt)
            };
            list.Add(button);
        }

        //Color is useless here but we still need to pass an argument, not great
        var rgbButton = new RadialMenuActionOption<Color>(EnergySwordRGB, _rgbEsword.Comp.ActivatedColor)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(_rgbEsword)
        };
        list.Add(rgbButton);

        _radialMenu.SetButtons(list);
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

    private Entity<EnergySwordComponent> SpawnEsword(Color color)
    {
        var meta = EntMan.GetComponent<MetaDataComponent>(Owner);
        Entity<EnergySwordComponent?> ent = EntMan.Spawn(meta.EntityPrototype!.ID);

        
        ent.Comp ??= EntMan.EnsureComponent<EnergySwordComponent>(ent);

        Entity<EnergySwordComponent> entity = (ent.Owner, ent.Comp);

        //Toggle the esword ON so the blade is out
        _eswordSystem.ActivateSword(entity);
        _eswordSystem.ChangeColor(entity, color);

        return entity;
    }

    private Entity<EnergySwordComponent> SpawnRgbEsword()
    {
        var esword = EntMan.GetComponent<EnergySwordComponent>(Owner);

        var swordEnt = SpawnEsword(esword.ColorOptions[0]);

        //RGB, color switching every second
        _eswordSystem.ActivateRGB(swordEnt);

        return swordEnt;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        //We need to delete the spawned eswords
        foreach (var esword in _colorEswords)
        {
            EntMan.DeleteEntity(esword.Owner);
        }

        EntMan.DeleteEntity(_rgbEsword);
    }
}
