using System;
using System.Collections.Generic;
using Content.Client.Items.Components;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Hands.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Content.Client.IoC.StaticIoC;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Items.UI
{
    public class ItemStatusPanel : Control
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables]
        private readonly List<(IItemStatus, Control)> _activeStatusComponents = new();

        [ViewVariables]
        private readonly Label _itemNameLabel;
        [ViewVariables]
        private readonly BoxContainer _statusContents;
        [ViewVariables]
        private readonly PanelContainer _panel;

        [ViewVariables]
        private EntityUid _entity;

        public ItemStatusPanel(Texture texture, StyleBox.Margin cutout, StyleBox.Margin flat, Label.AlignMode textAlign)
        {
            IoCManager.InjectDependencies(this);

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
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        SeparationOverride = 0,
                        Children =
                        {
                            (_statusContents = new BoxContainer
                            {
                                Orientation = LayoutOrientation.Vertical
                            }),
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
            VerticalAlignment = VAlignment.Bottom;

            // TODO: Depending on if its a two-hand panel or not
            MinSize = (150, 0);
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

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            UpdateItemName();
        }

        public void Update(EntityUid entity)
        {
            if (entity == default)
            {
                ClearOldStatus();
                _entity = default;
                _panel.Visible = false;
                return;
            }

            if (entity != _entity)
            {
                _entity = entity;
                BuildNewEntityStatus();

                UpdateItemName();
            }

            _panel.Visible = true;
        }

        private void UpdateItemName()
        {
            if (_entity == default)
                return;

            if (_entityManager.TryGetComponent(_entity, out HandVirtualItemComponent? virtualItem)
                && _entityManager.EntityExists(virtualItem.BlockingEntity))
            {
                _itemNameLabel.Text = _entityManager.GetComponent<MetaDataComponent>(virtualItem.BlockingEntity).EntityName;
            }
            else
            {
                _itemNameLabel.Text = _entityManager.GetComponent<MetaDataComponent>(_entity).EntityName;
            }
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

            foreach (var statusComponent in _entityManager.GetComponents<IItemStatus>(_entity))
            {
                var control = statusComponent.MakeControl();
                _statusContents.AddChild(control);

                _activeStatusComponents.Add((statusComponent, control));
            }

            var collectMsg = new ItemStatusCollectMessage();
            _entityManager.EventBus.RaiseLocalEvent(_entity, collectMsg);

            foreach (var control in collectMsg.Controls)
            {
                _statusContents.AddChild(control);
            }
        }
    }
}
