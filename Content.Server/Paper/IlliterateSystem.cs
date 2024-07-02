using System.Linq;
using Robust.Server.GameObjects;
using static Content.Shared.Paper.SharedPaperComponent;

namespace Content.Server.Paper
{
    public sealed class IlliterateSystem : EntitySystem
    {
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperComponent, PaperWrittenEvent>(OnPaperWritten);
            SubscribeLocalEvent<PaperComponent, PaperUpdatedEvent>(OnPaperUpdated);

        }

        private void OnPaperUpdated(Entity<PaperComponent> ent, ref PaperUpdatedEvent args)
        {
            var paperComp = ent.Comp;
            var newContent = new string("You can not understand anything on the paper");

            _uiSystem.SetUiState(ent.Owner, PaperUiKey.Key, new PaperBoundUserInterfaceState(newContent, paperComp.StampedBy, paperComp.Mode));
        }

        private void OnPaperWritten(Entity<PaperComponent> ent, ref PaperWrittenEvent args)
        {
            var newContent = new string(
                     ent.Comp.Content
                     .OrderBy(c => Guid.NewGuid())
                     .ToArray()
                     );

            _paper.SetContent(ent, newContent);
        }
    }
}
