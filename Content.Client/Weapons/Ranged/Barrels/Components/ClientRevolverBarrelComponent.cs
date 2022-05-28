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
    public sealed class ClientRevolverBarrelComponent : Component
    {
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
        }
    }
}
