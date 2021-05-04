using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Mobs.Speech;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    [DataDefinition]
    [RegisterComponent]
    public class BarnYardCurse : TimerComponent
    {
        public override string Name => "BarnYardCurse";

        public static Task DelayTask(IEntity entity, int milliseconds = 100, CancellationToken cancellationToken = default)
        {
            entity.AddComponent<CowAccentComponent>();
            return entity
                .EnsureComponent<TimerComponent>()
                .Delay(milliseconds, cancellationToken);
            
        }

    }
}
