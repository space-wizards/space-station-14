using Content.Server.GameObjects.Components.Suspicion;
using Content.Server.Interfaces.Chat;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Mobs.Roles
{
    public sealed class SuspicionTraitorRole : Role
    {
        public AntagPrototype Prototype { get; }

        public SuspicionTraitorRole(Mind mind, AntagPrototype antagPrototype) : base(mind)
        {
            Prototype = antagPrototype;
            Name = antagPrototype.Name;
            Antagonist = antagPrototype.Antagonist;
        }

        public override string Name { get; }
        public string Objective => Prototype.Objective;
        public override bool Antagonist { get; }

        public override void Greet()
        {
            base.Greet();

            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchServerMessage(Mind.Session, $"You're a {Name}!");
            chat.DispatchServerMessage(Mind.Session, $"Objective: {Objective}");

            var traitors = "";

            foreach (var sus in IoCManager.Resolve<IComponentManager>().EntityQuery<SuspicionRoleComponent>())
            {
                if (!sus.IsTraitor()) continue;
                if (traitors.Length > 0)
                    traitors += $", {sus.Owner.Name}";
                else
                    traitors += sus.Owner.Name;
            }
            
            chat.DispatchServerMessage(Mind.Session, $"The traitors are: {traitors}");
        }
    }
}
