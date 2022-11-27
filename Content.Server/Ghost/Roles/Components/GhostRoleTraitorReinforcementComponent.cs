using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.Ghost.Roles.Events;
using Content.Server.GameTicking.Rules;
using Content.Server.Humanoid.Systems;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Server.Ghost.Roles.Components
{
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public sealed class GhostRoleTraitorReinforcementComponent : GhostRoleComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("availableTakeovers")]
        private int _availableTakeovers = 1;

        [ViewVariables]
        private int _currentTakeovers = 0;

        [CanBeNull]
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("randomHumanoidSettings", customTypeSerializer: typeof(PrototypeIdSerializer<RandomHumanoidSettingsPrototype>))]
        public string? RandomHumanoidSettings { get; private set; }

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            if (string.IsNullOrEmpty(RandomHumanoidSettings))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            var randomHumanoid = _entMan.EntitySysManager.GetEntitySystem<RandomHumanoidSystem>();
            var mob = randomHumanoid.SpawnRandomHumanoid(RandomHumanoidSettings, _entMan.GetComponent<TransformComponent>(Owner).Coordinates, string.Empty);
            var xform = _entMan.GetComponent<TransformComponent>(mob);
            xform.AttachToGridOrMap();

            var spawnedEvent = new GhostRoleSpawnerUsedEvent(Owner, mob);
            _entMan.EventBus.RaiseLocalEvent(mob, spawnedEvent, false);

            if (MakeSentient)
                MakeSentientCommand.MakeSentient(mob, _entMan, AllowMovement, AllowSpeech);

            mob.EnsureComponent<MindComponent>();

            var ghostRoleSystem = _entMan.EntitySysManager.GetEntitySystem<GhostRoleSystem>();
            ghostRoleSystem.GhostRoleInternalCreateMindAndTransfer(session, Owner, mob, this);

            if (++_currentTakeovers < _availableTakeovers)
                return true;

            Taken = true;

            if (_currentTakeovers == _availableTakeovers)
                _entMan.QueueDeleteEntity(Owner);

            var traitorRuleSystem = _entMan.EntitySysManager.GetEntitySystem<TraitorRuleSystem>();
            traitorRuleSystem.MakeTraitor(session, false); // reinforcements don't get uplinks

            return true;
        }
    }
}
