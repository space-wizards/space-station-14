#nullable enable
using System.Collections.Generic;
using Content.Client.GameObjects.Components.Disposal;
using Content.Client.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Players;

namespace Content.Client.GameObjects.Components.Body
{
    [RegisterComponent]
    [ComponentReference(typeof(IDamageableComponent))]
    [ComponentReference(typeof(IBodyManagerComponent))]
    public class BodyManagerComponent : SharedBodyManagerComponent, IClientDraggable
    {
        // TODO
        public override List<DamageState> SupportedDamageStates { get; } = new List<DamageState>();

        public override DamageState CurrentDamageState { get; protected set; }

        public override int TotalDamage { get; } = 0;

        public override bool ChangeDamage(DamageType damageType, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null)
        {
            return true;
        }

        public override bool ChangeDamage(DamageClass damageClass, int amount, IEntity source, bool ignoreResistances,
            HealthChangeParams extraParams = null)
        {
            return true;
        }

        public override bool SetDamage(DamageType damageType, int newValue, IEntity source, HealthChangeParams extraParams = null)
        {
            return true;
        }

        public override void HealAllDamage()
        {
        }

        public override void ForceHealthChangedEvent()
        {
        }

        public bool ClientCanDropOn(CanDropEventArgs eventArgs)
        {
            return eventArgs.Target.HasComponent<DisposalUnitComponent>();
        }

        public bool ClientCanDrag(CanDragEventArgs eventArgs)
        {
            return true;
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession? session = null)
        {
            if (!Owner.TryGetComponent(out ISpriteComponent sprite))
            {
                return;
            }

            switch (message)
            {
                case BodyPartAddedMessage added:
                    sprite.LayerSetVisible(added.RSIMap, true);
                    sprite.LayerSetRSI(added.RSIMap, added.RSIPath);
                    sprite.LayerSetState(added.RSIMap, added.RSIState);
                    break;
                case BodyPartRemovedMessage removed:
                    sprite.LayerSetVisible(removed.RSIMap, false);
                    break;
            }
        }
    }
}
