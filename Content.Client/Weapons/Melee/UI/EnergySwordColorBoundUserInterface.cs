using Content.Client.UserInterface.Controls;
using Content.Shared.Actions.Components;
using Content.Shared.Weapons.Melee.EnergySword;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        EntProtoId<EnergySwordComponent> proto = new("EnergySword");
        Entity<EnergySwordComponent?> ent = EntMan.Spawn(proto);

        //No comp ?
        ent.Comp ??= EntMan.EnsureComponent<EnergySwordComponent>(ent);


        Entity<EnergySwordComponent> entity = (ent.Owner, ent.Comp);
        _eswordSystem.ActivateSword(entity);
        if (hacked)
            _eswordSystem.ActivateRGB(entity);
        else
            _eswordSystem.ChangeColor(entity, color);
        var button = new RadialMenuActionOption<Color>(PickColor, color)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(ent),
            //BackgroundColor = color.WithAlpha(140),
            //HoverBackgroundColor = color.WithAlpha(140)
        };

        return button;
    }

    private void PickColor(Color color)
    {
        SendPredictedMessage(new EnergySwordColorMessage(color));

        Close();
    }

}
