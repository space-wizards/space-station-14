using Content.Client.IoC;
using Content.Client.Items.Components;
using Content.Client.Resources;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Weapons.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public class ClientRevolverBarrelComponent : Component, IItemStatus
    {
        private StatusControl? _statusControl;

        /// <summary>
        /// A array that lists the bullet states
        /// true means a spent bullet
        /// false means a "shootable" bullet
        /// null means no bullet
        /// </summary>
        [ViewVariables]
        public bool?[] Bullets { get; private set; } = new bool?[0];

        [ViewVariables]
        public int CurrentSlot { get; private set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not RevolverBarrelComponentState cast)
                return;

            CurrentSlot = cast.CurrentSlot;
            Bullets = cast.Bullets;
            _statusControl?.Update();
        }

        public Control MakeControl()
        {
            _statusControl = new StatusControl(this);
            _statusControl.Update();
            return _statusControl;
        }

        public void DestroyControl(Control control)
        {
            if (_statusControl == control)
            {
                _statusControl = null;
            }
        }

        private sealed class StatusControl : Control
        {
            private readonly ClientRevolverBarrelComponent _parent;
            private readonly BoxContainer _bulletsList;

            public StatusControl(ClientRevolverBarrelComponent parent)
            {
                MinHeight = 15;
                _parent = parent;
                HorizontalExpand = true;
                VerticalAlignment = VAlignment.Center;
                AddChild((_bulletsList = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalExpand = true,
                    VerticalAlignment = VAlignment.Center,
                    SeparationOverride = 0
                }));
            }

            public void Update()
            {
                _bulletsList.RemoveAllChildren();

                var capacity = _parent.Bullets.Length;

                string texturePath;
                if (capacity <= 20)
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
                }
                else if (capacity <= 30)
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/small.png";
                }
                else
                {
                    texturePath = "/Textures/Interface/ItemStatus/Bullets/tiny.png";
                }

                var texture = StaticIoC.ResC.GetTexture(texturePath);
                var spentTexture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/empty.png");

                FillBulletRow(_bulletsList, texture, spentTexture);
            }

            private void FillBulletRow(Control container, Texture texture, Texture emptyTexture)
            {
                var colorA = Color.FromHex("#b68f0e");
                var colorB = Color.FromHex("#d7df60");
                var colorSpentA = Color.FromHex("#b50e25");
                var colorSpentB = Color.FromHex("#d3745f");
                var colorGoneA = Color.FromHex("#000000");
                var colorGoneB = Color.FromHex("#222222");

                var altColor = false;
                var scale = 1.3f;

                for (var i = 0; i < _parent.Bullets.Length; i++)
                {
                    var bulletSpent = _parent.Bullets[i];
                    // Add a outline
                    var box = new Control()
                    {
                        MinSize = texture.Size * scale,
                    };
                    if (i == _parent.CurrentSlot)
                    {
                        box.AddChild(new TextureRect
                        {
                            Texture = texture,
                            TextureScale = (scale, scale),
                            ModulateSelfOverride = Color.LimeGreen,
                        });
                    }
                    Color color;
                    Texture bulletTexture = texture;

                    if (bulletSpent.HasValue)
                    {
                        if (bulletSpent.Value)
                        {
                            color = altColor ? colorSpentA : colorSpentB;
                            bulletTexture = emptyTexture;
                        }
                        else
                        {
                            color = altColor ? colorA : colorB;
                        }
                    }
                    else
                    {
                        color = altColor ? colorGoneA : colorGoneB;
                    }

                    box.AddChild(new TextureRect
                    {
                        Stretch = TextureRect.StretchMode.KeepCentered,
                        Texture = bulletTexture,
                        ModulateSelfOverride = color,
                    });
                    altColor ^= true;
                    container.AddChild(box);
                }
            }
        }
    }
}
