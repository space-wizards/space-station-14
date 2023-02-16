using System.Linq;
using Content.Shared.AirlockPainter.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.AirlockPainter
{
    public abstract class SharedAirlockPainterSystem : EntitySystem
    {
        [Dependency] protected readonly IPrototypeManager _prototypeManager = default!;

        public List<string> Styles { get; private set; } = new();
        public List<AirlockGroupPrototype> Groups { get; private set; } = new();

        public override void Initialize()
        {
            base.Initialize();

            SortedSet<string> styles = new();
            foreach (AirlockGroupPrototype grp in _prototypeManager.EnumeratePrototypes<AirlockGroupPrototype>())
            {
                Groups.Add(grp);
                foreach (string style in grp.StylePaths.Keys)
                {
                    styles.Add(style);
                }
            }
            Styles = styles.ToList();
        }
    }
}
