using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Content.Shared.Recycling;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Recycling.Components
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    [Friend(typeof(RecyclerSystem))]
    public class RecyclerComponent : Component, ISuicideAct
    {
        public override string Name => "Recycler";

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("safe")]
        internal bool Safe = true;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("efficiency")]
        internal float Efficiency = 0.25f;

        private void Clean()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var mind = victim.PlayerSession()?.ContentData()?.Mind;

            if (mind != null)
            {
                EntitySystem.Get<GameTicker>().OnGhostAttempt(mind, false);
                mind.OwnedEntity?.PopupMessage(Loc.GetString("recycler-component-suicide-message"));
            }

            victim.PopupMessageOtherClients(Loc.GetString("recycler-component-suicide-message-others", ("victim",victim)));

            if (victim.TryGetComponent<SharedBodyComponent>(out var body))
            {
                body.Gib(true);
            }

            EntitySystem.Get<RecyclerSystem>().Bloodstain(this);

            return SuicideKind.Bloodloss;
        }
    }
}
