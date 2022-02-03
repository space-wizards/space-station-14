using System;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Ghost.Roles.Components
{
    /// <summary>
    ///     Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public class GhostRoleMobSpawnerComponent : GhostRoleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("deleteOnSpawn")]
        private bool _deleteOnSpawn = true;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("availableTakeovers")]
        private int _availableTakeovers = 1;

        [ViewVariables]
        private int _currentTakeovers = 0;

        [CanBeNull]
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? Prototype { get; private set; }

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            if (string.IsNullOrEmpty(Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var mob = _entMan.SpawnEntity(Prototype, _entMan.GetComponent<TransformComponent>(Owner).Coordinates);

            if (MakeSentient)
                MakeSentientCommand.MakeSentient(mob, _entMan);

            mob.EnsureComponent<MindComponent>();

            var ghostRoleSystem = EntitySystem.Get<GhostRoleSystem>();
            ghostRoleSystem.GhostRoleInternalCreateMindAndTransfer(session, Owner, mob, this);

            if (++_currentTakeovers < _availableTakeovers)
                return true;

            Taken = true;

            if (_deleteOnSpawn)
                _entMan.DeleteEntity(Owner);

            return true;
        }
    }
}
