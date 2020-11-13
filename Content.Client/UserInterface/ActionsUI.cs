using System;
using Content.Client.UserInterface.Stylesheets;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    /// <summary>
    ///     The action hotbar on the left side of the screen.
    /// </summary>
    public sealed class ActionsUI : Control
    {
        private readonly VBoxContainer _vbox;

        public ActionsUI()
        {
            var panelContainer = new PanelContainer
            {
                StyleClasses = {StyleNano.StyleClassBorderedWindowPanel},
                SizeFlagsVertical = SizeFlags.FillExpand,
            };
            AddChild(panelContainer);

            _vbox = new VBoxContainer() {SizeFlagsHorizontal = SizeFlags.FillExpand, SizeFlagsVertical = SizeFlags.FillExpand};
            panelContainer.AddChild(_vbox);

            LayoutContainer.SetGrowHorizontal(this, LayoutContainer.GrowDirection.Begin);
            LayoutContainer.SetAnchorAndMarginPreset(this, LayoutContainer.LayoutPreset.TopLeft, margin: 10);
            LayoutContainer.SetAnchorBottom(this, 1f);
            LayoutContainer.SetMarginTop(this, 250);
            LayoutContainer.SetMarginBottom(this, -250);

            // add the 9 slots
            for (var i = 0; i < 9; i++)
            {
                // TODO:
            }
            // TODO: 10th slot is labeled as 0
        }
    }
}
