using Content.Server.UserInterface;
using Content.Shared.Sound;
using Content.Shared.SmartFridge;
using Robust.Server.GameObjects;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Server.SmartFridge
{
    [RegisterComponent, Friend(typeof(SmartFridgeSystem))]
    public class SmartFridgeComponent : SharedSmartFridgeComponent
    {
        public bool Ejecting;
        public TimeSpan AnimationDuration = TimeSpan.Zero;
        [DataField("pack")]

        public bool Broken;

        [DataField("soundVend")]
        // Grabbed from: https://github.com/discordia-space/CEV-Eris/blob/f702afa271136d093ddeb415423240a2ceb212f0/sound/machines/vending_drop.ogg
        public SoundSpecifier SoundVend = new SoundPathSpecifier("/Audio/Machines/machine_vend.ogg");
        [DataField("soundDeny")]
        // Yoinked from: https://github.com/discordia-space/CEV-Eris/blob/35bbad6764b14e15c03a816e3e89aa1751660ba9/sound/machines/Custom_deny.ogg
        public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(SmartFridgeUiKey.Key);

        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;
        public Container? Storage = default!;
        public Dictionary<uint,  Queue<EntityUid>> entityReference = new();
    }
}

