using Content.Server.GameObjects.Components.Observer;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Mobs;
using Content.Server.Players;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    ///     Stores a <see cref="Server.Mobs.Mind"/> on a mob.
    /// </summary>
    [RegisterComponent]
    public class MindComponent : Component, IExamine
    {
        /// <inheritdoc />
        public override string Name => "Mind";

        /// <summary>
        ///     The mind controlling this mob. Can be null.
        /// </summary>
        [ViewVariables]
        public Mind Mind { get; private set; }

        /// <summary>
        ///     True if we have a mind, false otherwise.
        /// </summary>
        [ViewVariables]
        public bool HasMind => Mind != null;

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalEjectMind()
        {
            Mind = null;
        }

        /// <summary>
        ///     Don't call this unless you know what the hell you're doing.
        ///     Use <see cref="Mind.TransferTo(IEntity)"/> instead.
        ///     If that doesn't cover it, make something to cover it.
        /// </summary>
        public void InternalAssignMind(Mind value)
        {
            Mind = value;
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            if (HasMind)
            {
                var visiting = Mind.VisitingEntity;
                if (visiting != null)
                {
                    if (visiting.TryGetComponent(out GhostComponent ghost))
                    {
                        ghost.CanReturnToBody = false;
                    }

                    Mind.TransferTo(visiting);
                }
                else
                {
                    var ghost = Owner.EntityManager.SpawnEntity("MobObserver", Owner.Transform.GridPosition);
                    ghost.Name = Mind.CharacterName;

                    var ghostComponent = ghost.GetComponent<GhostComponent>();
                    ghostComponent.CanReturnToBody = false;
                    Mind.TransferTo(ghost);
                }
            }
        }

        public void Examine(FormattedMessage message)
        {
            // TODO: Use gendered pronouns depending on the entity
            if(!HasMind)
                message.AddMarkup($"[color=red]They are totally catatonic. The stresses of life in deep-space must have been too much for them. Any recovery is unlikely.[/color]");
            else if(Mind.Session == null)
                message.AddMarkup("[color=yellow]They have a blank, absent-minded stare and appears completely unresponsive to anything. They may snap out of it soon.[/color]");
        }
    }
}
