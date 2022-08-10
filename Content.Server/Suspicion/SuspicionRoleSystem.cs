using Content.Shared.Examine;

namespace Content.Server.Suspicion
{
    public sealed class SuspicionRoleSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SuspicionRoleComponent, ExaminedEvent>(OnExamined);
        }
        private void OnExamined(EntityUid uid, SuspicionRoleComponent component, ExaminedEvent args)
        {
           if (!component.IsDead())
            {
                return;
            }

            var traitor = component.IsTraitor();
            var color = traitor ? "red" : "green";
            var role = traitor ? "suspicion-role-component-role-traitor" : "suspicion-role-component-role-innocent";
            var article = traitor ? "generic-article-a" : "generic-article-an";

            var tooltip = Loc.GetString("suspicion-role-component-on-examine-tooltip",
                                        ("article", Loc.GetString(article)),
                                        ("colorName", color),
                                        ("role",Loc.GetString(role)));

            args.PushMarkup(tooltip);
        }
    }
}
