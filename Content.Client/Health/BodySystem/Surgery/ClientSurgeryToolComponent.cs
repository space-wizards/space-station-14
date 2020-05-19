using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Storage;
using Content.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Content.Shared.BodySystem;
using System.Globalization;

namespace Content.Client.BodySystem
{
    [RegisterComponent]
    public class ClientSurgeryToolComponent : SharedSurgeryToolComponent
    {
        private SurgeryToolWindow Window; 
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case OpenSurgeryUIMessage msg:
                    HandleOpenSurgeryUIMessage();
                    break;
                case CloseSurgeryUIMessage msg:
                    HandleCloseSurgeryUIMessage();
                    break;
                case UpdateSurgeryUIMessage msg:
                    HandleUpdateSurgeryUIMessage(msg);
                    break;
            }
        }
        public override void OnAdd()
        {
            base.OnAdd();
            Window = new SurgeryToolWindow() { SurgeryToolEntity = this };
        }

        public override void OnRemove()
        {
            Window.Dispose();
            base.OnRemove();
        }
        
        private void HandleOpenSurgeryUIMessage()
        {
            Window.Open();
        }
        private void HandleCloseSurgeryUIMessage()
        {
            Window.Close();
        }
        private void HandleUpdateSurgeryUIMessage(UpdateSurgeryUIMessage surgeryUIState)
        {
            Window.BuildDisplay(surgeryUIState.Targets);
        }
        private class SurgeryToolWindow : SS14Window
        {
            private Control _VSplitContainer;
            private VBoxContainer _bodyPartList;
            public ClientSurgeryToolComponent SurgeryToolEntity;

            protected override Vector2? CustomSize => (300, 400);

            public SurgeryToolWindow()
            {
                Title = "Select surgery target...";
                RectClipContent = true;

                _VSplitContainer = new VBoxContainer();
                var listScrollContainer = new ScrollContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    HScrollEnabled = true,
                    VScrollEnabled = true
                };
                _bodyPartList = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };
                listScrollContainer.AddChild(_bodyPartList);
                _VSplitContainer.AddChild(listScrollContainer);
                Contents.AddChild(_VSplitContainer);
            }

            public override void Close()
            {
                SurgeryToolEntity.SendNetworkMessage(new CloseSurgeryUIMessage());
                base.Close();
            }

            public void BuildDisplay(Dictionary<string, string> targets)
            {
                _bodyPartList.DisposeAllChildren();
                foreach (var(slotName, partname) in targets)
                {
                    var button = new BodyPartButton(slotName);
                    button.ActualButton.OnToggled += OnButtonPressed;
                    button.LimbName.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(slotName + " - " + partname);

                    //button.SpriteView.Sprite = sprite;

                    _bodyPartList.AddChild(button);
                }
            }

            private void OnButtonPressed(BaseButton.ButtonEventArgs args)
            {
                var parent = (BodyPartButton) args.Button.Parent;
                SurgeryToolEntity.SendNetworkMessage(new SelectSurgeryUIMessage(parent.LimbSlotName));
            }
        }

        private class BodyPartButton : PanelContainer
        {
            public Button ActualButton { get; }
            public SpriteView SpriteView { get; }
            public Control EntityControl { get; }
            public Label LimbName { get; }
            public string LimbSlotName { get; }

            public BodyPartButton(string slotName)
            {
                LimbSlotName = slotName;
                ActualButton = new Button
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand,
                    SizeFlagsVertical = SizeFlags.FillExpand,
                    ToggleMode = true,
                    MouseFilter = MouseFilterMode.Stop
                };
                AddChild(ActualButton);

                var hBoxContainer = new HBoxContainer();
                SpriteView = new SpriteView
                {
                    CustomMinimumSize = new Vector2(32.0f, 32.0f)
                };
                LimbName = new Label
                {
                    SizeFlagsVertical = SizeFlags.ShrinkCenter,
                    Text = "N/A",
                };
                hBoxContainer.AddChild(SpriteView);
                hBoxContainer.AddChild(LimbName);

                EntityControl = new Control
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };
                hBoxContainer.AddChild(EntityControl);
                AddChild(hBoxContainer);
            }
        }
    }
}
