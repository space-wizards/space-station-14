using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public class SeedComponent : Component, IExamine
#pragma warning restore 618
    {
        [DataField("seed")]
        private string? _seedName;

        [ViewVariables]
        public Seed? Seed
        {
            get => _seedName != null ? IoCManager.Resolve<IPrototypeManager>().Index<Seed>(_seedName) : null;
            set => _seedName = value?.ID;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;

            if (Seed == null)
            {
                message.AddMarkup(Loc.GetString("seed-component-no-seeds-message") + "\n");
                return;
            }

            message.AddMarkup(Loc.GetString($"seed-component-description", ("seedName", Seed.DisplayName)) + "\n");

            if (!Seed.RoundStart)
            {
                message.AddMarkup(Loc.GetString($"seed-component-has-variety-tag", ("seedUid", Seed.Uid)) + "\n");
            }
            else
            {
                message.AddMarkup(Loc.GetString($"seed-component-plant-yield-text", ("seedYield", Seed.Yield)) + "\n");
                message.AddMarkup(Loc.GetString($"seed-component-plant-potency-text", ("seedPotency", Seed.Potency)) + "\n");
            }
        }
    }
}
