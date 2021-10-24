using System;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Respiratory;
using Content.Server.Explosion;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Behaviors.Item;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Audio;
using Content.Shared.DragDrop;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Content.Shared.Radiation;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Analyzers;


namespace Content.Server.Supermatter
{
    [RegisterComponent]
    public class SupermatterComponent : Component
    {
        public override string Name => "Supermatter";
        private int _energy = 1;
        //private float _range = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        public int Energy
        {
            get => _energy;
            set
            {
                _energy = value;
                Dirty();
            }
        }
    }
}
