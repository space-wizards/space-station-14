#nullable enable
using Content.Server.Observer;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedBodyComponent))]
    [ComponentReference(typeof(IBody))]
    public class BodyComponent : SharedBodyComponent, IRelayMoveInput
    {
        protected override void Startup()
        {
            base.Startup();

            // This is ran in Startup as entities spawned in Initialize
            // are not synced to the client since they are assumed to be
            // identical on it
            foreach (var (slot, partId) in PartIds)
            {
                // Using MapPosition instead of Coordinates here prevents
                // a crash within the character preview menu in the lobby
                var entity = Owner.EntityManager.SpawnEntity(partId, Owner.Transform.MapPosition);

                if (!entity.TryGetComponent(out IBodyPart? part))
                {
                    Logger.Error($"Entity {partId} does not have a {nameof(IBodyPart)} component.");
                    continue;
                }

                TryAddPart(slot, part, true);
            }
        }

        void IRelayMoveInput.MoveInputPressed(ICommonSession session)
        {
            if (Owner.TryGetComponent(out IDamageableComponent? damageable) &&
                damageable.CurrentDamageState == DamageState.Dead)
            {
                new Ghost().Execute(null, (IPlayerSession) session, null);
            }
        }
    }
}
