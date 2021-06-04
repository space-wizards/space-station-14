#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : Component
    {
        public override string Name => "Flash";

        [DataField("duration")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int FlashDuration = 5000;

        [DataField("uses")]
        [ViewVariables(VVAccess.ReadWrite)]
        public int Uses = 5;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Range = 7f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("aoeFlashDuration")]
        public int AoeFlashDuration = 2000;

        [DataField("slowTo")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowTo = 0.5f;

        public bool Flashing;

        public bool HasUses => Uses > 0;

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!HasUses)
            {
                message.AddText(Loc.GetString("flash-component-examine-empty"));
                return;
            }

            if (inDetailsRange)
            {
                message.AddMarkup(
                    Loc.GetString(
                        "flash-component-examine-detail-count",
                        ("count", Uses),
                        ("markupCountColor", "green")
                    )
                );
            }
        }
    }
}
