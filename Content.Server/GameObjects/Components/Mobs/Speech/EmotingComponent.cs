using System;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class EmotingComponent : Component, IActionBlocker
    {
        public override string Name => "Emoting";

        public bool Enabled { get; set; } = true;

        bool IActionBlocker.CanEmote() => Enabled;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Enabled, "enabled", true);
        }
    }
}
