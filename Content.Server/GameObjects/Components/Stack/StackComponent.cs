using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Stack
{
    // TODO: Naming and presentation and such could use some improvement.
    [RegisterComponent]
    [ComponentReference(typeof(SharedStackComponent))]
    public class StackComponent : SharedStackComponent, IExamine
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ThrowIndividually { get; set; } = false;

        void IExamine.Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (inDetailsRange)
            {
                message.AddMarkup(
                    Loc.GetString(
                        "comp-stack-examine-detail-count",
                        ("count", Count),
                        ("markupCountColor", "lightgray")
                    )
                );
            }
        }
    }
}
