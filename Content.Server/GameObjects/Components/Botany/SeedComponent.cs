#nullable enable
using Content.Server.Botany;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Botany
{
    [RegisterComponent]
    public class SeedComponent : Component, IExamine
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "Seed";

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
                message.AddMarkup(Loc.GetString("It doesn't seem to contain any seeds.\n"));
                return;
            }

            message.AddMarkup(Loc.GetString($"It has a picture of [color=yellow]{Seed.DisplayName}[/color] on the front.\n"));

            if(!Seed.RoundStart)
                message.AddMarkup(Loc.GetString($"It's tagged as variety [color=lightgray]no. {Seed.Uid}[/color].\n"));
            else
            {
                message.AddMarkup(Loc.GetString($"Plant Yield:    [color=lightblue]{Seed.Yield}[/color]\n"));
                message.AddMarkup(Loc.GetString($"Plant Potency: [color=lightblue]{Seed.Potency}[/color]\n"));
            }
        }
    }
}
