using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Players;
using JetBrains.Annotations;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Observer
{
    /// <summary>
    ///     Allows a ghost to take this role, spawning a new entity.
    /// </summary>
    [RegisterComponent, ComponentReference(typeof(GhostRoleComponent))]
    public class GhostRoleMobSpawnerComponent : GhostRoleComponent
    {
        [Dependency] private readonly IServerEntityManager _entityMan = default!;

        public override string Name => "GhostRoleMobSpawner";
        [CanBeNull] public string Prototype { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => Prototype, "prototype", null);
        }

        public override bool Take(IPlayerSession session)
        {
            if (Taken)
                return false;

            if(string.IsNullOrEmpty(Prototype))
                throw new NullReferenceException("Prototype string cannot be null or empty!");

            Taken = true;
            var mob = _entityMan.SpawnEntity(Prototype, Owner.Transform.GridPosition);

            mob.EnsureComponent<MindComponent>();
            session.ContentData().Mind.TransferTo(mob);

            Owner.Delete();

            return true;
        }
    }
}
