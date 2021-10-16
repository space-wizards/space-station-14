using System;
using Content.Shared.Movement.Components;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Stunnable
{
    [Friend(typeof(SharedStunSystem))]
    [RegisterComponent, NetworkedComponent]
    public sealed class StunnedComponent : Component
    {
        public sealed override string Name => "Stunned";
    }
}
