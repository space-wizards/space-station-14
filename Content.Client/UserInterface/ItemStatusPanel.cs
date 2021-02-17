#nullable enable
using System;
using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.Components.Items;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Client.StaticIoC;

namespace Content.Client.UserInterface
{
    public class ItemStatusPanel : Control
    {
        [ViewVariables]
        private readonly List<(IItemStatus, Control)> _activeStatusComponents = new();

        [ViewVariables]
        private readonly Label _itemNameLabel;
        [ViewVariables]
        private readonly VBoxContainer _statusContents;
        [ViewVariables]
        private readonly PanelContainer _panel;

        [ViewVariables]
        private IEntity? _entity;

        public ItemStatusPanel(Texture texture, StyleBox.Margin cutout, StyleBox.Margin flat, Label.AlignMode textAlign)
        {
            var panel = new StyleBoxTexture
            {
                Texture = texture
            };
            panel.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
            panel.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
            panel.SetPatchMargin(flat, 2);
            panel.SetPatchMargin(cutout, 13);

            AddChild(_panel = new PanelContainer
            {
                PanelOverride = panel,
                ModulateSelfOverride = Color.White.WithAlpha(0.9f),
                Children =
                {
                    new VBoxContainer
                    {
                        SeparationOverride = 0,
                        Children =
                        {
                            (_statusContents = new VBoxContainer()),
                            (_itemNameLabel = new Label
                            {
                                ClipText = true,
                                StyleClasses = {StyleNano.StyleClassItemStatus},
                                Align = textAlign
                            })
                        }
                    }
                }
            });
            SizeFlagsVertical = SizeFlags.ShrinkEnd;
        }

        /// <summary>
        ///     Creates a new instance of <see cref="ItemStatusPanel"/>
        ///     based on whether or not it is being created for the right
        ///     or left hand.
        /// </summary>
        /// <param name="location">
        ///     The location of the hand that this panel is for
        /// </param>
        /// <returns>the new <see cref="ItemStatusPanel"/> instance</returns>
        public static ItemStatusPanel FromSide(HandLocation location)
        {
            string texture;
            StyleBox.Margin cutOut;
            StyleBox.Margin flat;
            Label.AlignMode textAlign;

            switch (location)
            {
                case HandLocation.Left:
                    texture = "/Textures/Interface/Nano/item_status_right.svg.96dpi.png";
                    cutOut = StyleBox.Margin.Left | StyleBox.Margin.Top;
                    flat = StyleBox.Margin.Right | StyleBox.Margin.Bottom;
                    textAlign = Label.AlignMode.Right;
                    break;
                case HandLocation.Middle:
                    texture = "/Textures/Interface/Nano/item_status_middle.svg.96dpi.png";
                    cutOut = StyleBox.Margin.Right | StyleBox.Margin.Top;
                    flat = StyleBox.Margin.Left | StyleBox.Margin.Bottom;
                    textAlign = Label.AlignMode.Left;
                    break;
                case HandLocation.Right:
                    texture = "/Textures/Interface/Nano/item_status_left.svg.96dpi.png";
                    cutOut = StyleBox.Margin.Right | StyleBox.Margin.Top;
                    flat = StyleBox.Margin.Left | StyleBox.Margin.Bottom;
                    textAlign = Label.AlignMode.Left;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }

            return new ItemStatusPanel(ResC.GetTexture(texture), cutOut, flat, textAlign);
        }

        public void Update(IEntity? entity)
        {
            if (entity == null)
            {
                ClearOldStatus();
                _entity = null;
                _panel.Visible = false;
                return;
            }

            if (entity != _entity)
            {
                _entity = entity;
                BuildNewEntityStatus();
            }

            _panel.Visible = true;
            _itemNameLabel.Text = entity.Name;
        }

        private void ClearOldStatus()
        {
            _statusContents.RemoveAllChildren();

            foreach (var (itemStatus, control) in _activeStatusComponents)
            {
                itemStatus.DestroyControl(control);
            }

            _activeStatusComponents.Clear();
        }

        private void BuildNewEntityStatus()
        {
            DebugTools.AssertNotNull(_entity);

            ClearOldStatus();

            foreach (var statusComponent in _entity!.GetAllComponents<IItemStatus>())
            {
                var control = statusComponent.MakeControl();
                _statusContents.AddChild(control);

                _activeStatusComponents.Add((statusComponent, control));
            }
        }

        // TODO: Depending on if its a two-hand panel or not
        protected override Vector2 CalculateMinimumSize()
        {
            return Vector2.ComponentMax(base.CalculateMinimumSize(), (150, 0));
        }
    }
}
