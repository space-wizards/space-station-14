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
            EntProtoId<EnergySwordComponent> proto = new("EnergySword");
            Entity<EnergySwordComponent?> ent = EntMan.Spawn(proto);

            //No comp ?
            ent.Comp ??= EntMan.EnsureComponent<EnergySwordComponent>(ent);


            Entity<EnergySwordComponent> entity = (ent.Owner, ent.Comp);
            //_eswordSystem.ActivateSword(entity);
            _eswordSystem.ChangeColor(entity, color);
            var button = new RadialMenuActionOption<Color>(PickColor, color)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(ent),
                BackgroundColor = color.WithAlpha(140),
                HoverBackgroundColor = color.WithAlpha(140)
            };
            list.Add(button);
        }

        //Add rgb option
        list.Add(new RadialMenuActionOption<Color>(PickColor, Color.White)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(Owner),
            BackgroundColor = Color.White.WithAlpha(140),
            HoverBackgroundColor = Color.White.WithAlpha(140)
        });
        _radialMenu.SetButtons(list);
    }

    private void PickColor(Color color)
    {
        SendPredictedMessage(new EnergySwordColorMessage(color));

        Close();
    }
}
