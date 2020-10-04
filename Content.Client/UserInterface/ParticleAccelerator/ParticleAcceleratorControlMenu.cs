using System;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;

namespace Content.Client.ParticleAccelerator
{
    public class ParticleAcceleratorControlMenu : SS14Window
    {
        private ParticleAcceleratorBoundUserInterface Owner;
        private VBoxContainer _assemblyScreenRootContainer;

        private VBoxContainer _workingScreenRootContainer;
        private Label _powerIndicatorLabel;

        public ParticleAcceleratorControlMenu(ParticleAcceleratorBoundUserInterface owner)
        {
            Owner = owner;
            _assemblyScreenRootContainer = new VBoxContainer();
            _assemblyScreenRootContainer.AddChild(new Label{Text = "Not all parts found"});
            _assemblyScreenRootContainer.AddChild(new Label{Text = "Soon i will show you here what components are already in there"});
            _assemblyScreenRootContainer.AddChild(new Button{Text = "Nonfunctional (for now) rescan button"});

            _workingScreenRootContainer = new VBoxContainer();
            var toggleButton = new Button {SizeFlagsHorizontal = SizeFlags.ShrinkCenter, Text = "Toggle Power"};
            toggleButton.OnPressed += args => Owner.SendToggleMessage();
            _workingScreenRootContainer.AddChild(toggleButton);
            var powerAdjustContainer = new HBoxContainer{SizeFlagsHorizontal = SizeFlags.ShrinkCenter};
            var powerDecreaseButton = new Button{Text = "-"};
            powerDecreaseButton.OnPressed += args => Owner.SendDecreaseMessage();
            powerAdjustContainer.AddChild(powerDecreaseButton);
            _powerIndicatorLabel = new Label();
            powerAdjustContainer.AddChild(_powerIndicatorLabel);
            var powerIncreaseButton = new Button{Text = "+"};
            powerIncreaseButton.OnPressed += args => Owner.SendIncreaseMessage();
            powerAdjustContainer.AddChild(powerIncreaseButton);
            _workingScreenRootContainer.AddChild(powerAdjustContainer);

            Contents.AddChild(_assemblyScreenRootContainer);
        }

        public void DataUpdate(ParticleAcceleratorDataUpdateMessage dataUpdateMessage)
        {
            Contents.RemoveAllChildren();
            if (dataUpdateMessage.Assembled)
            {
                Contents.AddChild(_workingScreenRootContainer);
                _powerIndicatorLabel.Text = dataUpdateMessage.State switch
                {
                    ParticleAcceleratorPowerState.Off => "X",
                    ParticleAcceleratorPowerState.Powered => "-",
                    ParticleAcceleratorPowerState.Level0 => "0",
                    ParticleAcceleratorPowerState.Level1 => "1",
                    ParticleAcceleratorPowerState.Level2 => "2",
                    ParticleAcceleratorPowerState.Level3 => "3",
                    _ => "E"
                };
            }
            else
            {
                Contents.AddChild(_assemblyScreenRootContainer);
            }
        }
    }
}
