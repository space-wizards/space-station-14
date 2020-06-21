using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    // Yeah I put it all in the same enum, don't judge me
    public enum RangedBarrelVisualLayers
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

        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is RangedWeaponComponentState rangedState))
            {
                return;
            }

            FireRateSelector = rangedState.FireRateSelector;
        }

        public void SyncFirePos(GridCoordinates worldPos)
        {
            SendNetworkMessage(new FirePosComponentMessage(worldPos));
        }
    }
}