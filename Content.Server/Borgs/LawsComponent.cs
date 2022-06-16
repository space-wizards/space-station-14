using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Server.Borgs
{
    [RegisterComponent]
    public sealed class LawsComponent : Component
    {
        [DataField("laws")]
        public HashSet<string> Laws = default!;

        [DataField("stateLawsAction")]
        public InstantAction StateLawsAction = new()
        {
            UseDelay = TimeSpan.FromSeconds(10),
            Icon = new SpriteSpecifier.Texture(new ResourcePath("Structures/Wallmounts/posters.rsi/poster11_legit.png")),
            Name = "state-laws-action",
            Description = "state-laws-action-desc",
            Priority = -1,
            Event = new StateLawsActionEvent(),
        };
    }
}
