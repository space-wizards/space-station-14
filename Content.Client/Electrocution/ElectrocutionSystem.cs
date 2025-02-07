using Robust.Shared.Utility;
using Robust.Client.GameObjects;
using Content.Shared.Electrocution;
using Content.Shared.Doors.Components;

namespace Content.Client.Electrocution
{
    public sealed class ElectrocutionSystem : SharedElectrocutionSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ElectrifiedComponent, ComponentInit>(OnComponentInit);
        }
        
        private void OnComponentInit(EntityUid uid, ElectrifiedComponent component, ComponentInit args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite) || !HasComp<DoorComponent>(uid))
                return;
            
            if (sprite.BaseRSI?.TryGetState("electrified", out var state) ?? true)
                return;
            
            var index = sprite.LayerMapReserveBlank("electrified");

            sprite.LayerSetRSI(index, "/Textures/Interface/Misc/ai_hud.rsi");
            sprite.LayerSetState(index, "electrified");
            sprite.LayerSetVisible(index, false);
            sprite.LayerMapSet(ElectrifiedLayers.HUD, index);
        }
    }
}
