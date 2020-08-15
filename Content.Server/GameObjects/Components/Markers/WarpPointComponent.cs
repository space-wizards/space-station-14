using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public sealed class WarpPointComponent : Component, IExamine
    {
        public override string Name => "WarpPoint";

        [ViewVariables(VVAccess.ReadWrite)] public string Location { get; set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Location, "location", null);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var loc = Location == null ? "<null>" : $"'{Location}'";
            message.AddText(Loc.GetString("This one's location ID is {0}", loc));
        }
    }
}
