#nullable enable
using System.Collections.Generic;
using Content.Client.GameObjects.Components;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Client.StaticIoC;

namespace Content.Client.UserInterface
{
    public class ItemStatusPanel : Control
    {
        [ViewVariables]
        private readonly List<(IItemStatus, Control)> _activeStatusComponents = new List<(IItemStatus, Control)>();

        [ViewVariables]
        private readonly Label _itemNameLabel;
        [ViewVariables]
        private readonly VBoxContainer _statusContents;
        [ViewVariables]
        private readonly PanelContainer _panel;

        [ViewVariables]
        private IEntity? _entity;

        public ItemStatusPanel(Texture texture, StyleBox.Margin margin)
        {
            var panel = new StyleBoxTexture
            {
                Texture = texture
            };
            panel.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
            panel.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
            panel.SetPatchMargin(margin, 13);

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
                                StyleClasses = {StyleNano.StyleClassItemStatus}
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
        /// <param name="isRightHand">
        ///     true if this is the right hand, which is drawn on the
        ///     LEFT side of the screen
        /// </param>
        /// <returns>the new <see cref="ItemStatusPanel"/> instance</returns>
        public static ItemStatusPanel FromSide(bool isRightHand)
        {
            var texture = ResC.GetTexture(isRightHand
                ? "/Nano/item_status_right.svg.96dpi.png"
                : "/Nano/item_status_left.svg.96dpi.png");
            var margin = (isRightHand
                ? StyleBox.Margin.Left
                : StyleBox.Margin.Right) | StyleBox.Margin.Top;

            return new ItemStatusPanel(texture, margin);
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
            return Vector2.ComponentMax(base.CalculateMinimumSize(), (150, 15));
        }
    }
}
