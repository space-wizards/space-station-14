using Content.Client.Items;
using Content.Client.Items.Components;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Client.IoC.StaticIoC;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.UserInterface.Systems.Inventory.Controls
{
    public sealed class ItemStatusPanel : Popup
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
        private EntityUid? _entity;

        public ItemStatusPanel()
        {
            IoCManager.InjectDependencies(this);

            var panel = new StyleBoxTexture();
            panel.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
            panel.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);

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
                                StyleClasses = {StyleNano.StyleClassItemStatus}
                            })
                        }
                    }
                }
            });
            VerticalAlignment = VAlignment.Bottom;

            // TODO: Depending on if its a two-hand panel or not
            MinSize = (150, 0);

            SetSide(HandLocation.Middle);
        }

        public void SetSide(HandLocation location)
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

            var panel = (StyleBoxTexture) _panel.PanelOverride!;
            panel.Texture = ResC.GetTexture(texture);
            panel.SetPatchMargin(flat, 2);
            panel.SetPatchMargin(cutOut, 13);

            _itemNameLabel.Align = textAlign;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            UpdateItemName();
        }

        public void Update(EntityUid? entity)
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
                _entity = entity.Value;
                BuildNewEntityStatus();

                UpdateItemName();
            }

            _panel.Visible = true;
        }

        private void UpdateItemName()
        {
            if (_entity == null)
                return;

            if (_entityManager.TryGetComponent(_entity, out HandVirtualItemComponent? virtualItem)
                && _entityManager.EntityExists(virtualItem.BlockingEntity))
            {
                // Uses identity because we can be blocked by pulling someone
                _itemNameLabel.Text = Identity.Name(virtualItem.BlockingEntity, _entityManager);
            }
            else
            {
                _itemNameLabel.Text = Identity.Name(_entity.Value, _entityManager);
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

            foreach (var statusComponent in _entityManager.GetComponents<IItemStatus>(_entity!.Value))
            {
                var control = statusComponent.MakeControl();
                _statusContents.AddChild(control);

                _activeStatusComponents.Add((statusComponent, control));
            }

            var collectMsg = new ItemStatusCollectMessage();
            _entityManager.EventBus.RaiseLocalEvent(_entity!.Value, collectMsg, true);

            foreach (var control in collectMsg.Controls)
            {
                _statusContents.AddChild(control);
            }
        }
    }
}
