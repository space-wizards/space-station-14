using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    // Yeah I put it all in the same enum, don't judge me
    public enum RangedBarrelVisualLayers : byte
    {
        Base,
        BaseUnshaded,
        Bolt,
        Mag,
        MagUnshaded,
    }

    [RegisterComponent]
    public sealed class ClientRangedWeaponComponent : SharedRangedWeaponComponent
    {
        public FireRateSelector FireRateSelector { get; private set; } = FireRateSelector.Safety;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not RangedWeaponComponentState rangedState)
            {
                return;
            }

            FireRateSelector = rangedState.FireRateSelector;
        }

        public void SyncFirePos(GridId targetGrid, Vector2 targetPosition)
        {
            SendNetworkMessage(new FirePosComponentMessage(targetGrid, targetPosition));
        }
    }
}
