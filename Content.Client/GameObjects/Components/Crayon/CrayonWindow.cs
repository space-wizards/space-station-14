using Content.Shared.GameObjects.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using SixLabors.ImageSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Content.Client.GameObjects.Components.Crayon
{
    public class CrayonWindow : SS14Window
    {
        public CrayonBoundUserInterface Owner { get; }
        private readonly LineEdit _search;
        private readonly ItemList _decalList;
        private readonly Button _selectButton;
        private Dictionary<string, Texture> _decals;

        protected override Vector2? CustomSize => (250, 300);

        public CrayonWindow(CrayonBoundUserInterface owner)
        {
            Title = "Crayon";
            Owner = owner;

            var vbox = new VBoxContainer();
            Contents.AddChild(vbox);

            _search = new LineEdit();
            _search.OnTextChanged += (e) => RefreshList();
            vbox.AddChild(_search);

            var margin = new MarginContainer()
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                SizeFlagsHorizontal = SizeFlags.FillExpand,
            };
            _decalList = new ItemList();
            margin.AddChild(_decalList);
            vbox.AddChild(margin);

            _selectButton = new Button()
            {
                Text = "Select"
            };
            _selectButton.OnPressed += Select;
            vbox.AddChild(_selectButton);
        }

        private void Select(BaseButton.ButtonEventArgs obj)
        {
            var selected = _decalList.GetSelected().FirstOrDefault();
            if (selected == null)
                return;

            Owner.Select(selected.Text);
        }

        private void RefreshList()
        {
            // Clear
            _decalList.Clear();
            if (_decals == null)
                return;

            var filter = _search.Text;
            foreach (var (decal, tex) in _decals)
            {
                if (!decal.Contains(filter))
                    continue;
                
                _decalList.AddItem(decal, tex);
            }
        }

        public void Populate(CrayonDecalPrototype proto)
        {
            var path = new ResourcePath(proto.SpritePath);
            _decals = new Dictionary<string, Texture>();
            foreach (var state in proto.Decals)
            {
                var rsi = new SpriteSpecifier.Rsi(path, state);
                _decals.Add(state, rsi.Frame0());
            }
            
            RefreshList();
        }
    }
}
