using System;
using Content.Client.UserInterface;
using Content.Client.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components
{
    [RegisterComponent]
    public class ToggleComponent : Component
    {
        public override string Name => "Toggle";
        public override uint? NetID => ContentNetIDs.TOGGLE;

        [ViewVariables] public float AmmountCapacity { get; private set; }
        [ViewVariables] public float Ammount { get; private set; }
        [ViewVariables] public bool Activated { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)] private bool _uiUpdateNeeded;

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (!(curState is ToggleComponentState cast))
                return;

            AmmountCapacity = cast.AmmountCapacity;
            Ammount = cast.Ammount;
            Activated = cast.Activated;

            _uiUpdateNeeded = true;
        }
    }
}
