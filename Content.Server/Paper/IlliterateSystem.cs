using System.Linq;

namespace Content.Server.Paper
{
    public sealed class IlliterateSystem : EntitySystem
    {
        [Dependency] private readonly PaperSystem _paper = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PaperComponent, PaperWrittenEvent>(OnPaperWritten);
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
