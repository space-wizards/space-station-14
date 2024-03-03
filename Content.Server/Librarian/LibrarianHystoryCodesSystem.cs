
using Content.Server.GameTicking.Events;
using Content.Shared.Librarian;
using Robust.Shared.Prototypes;

namespace Content.Server.Librarian;


public sealed class LibrarianHistoryCodesSystem : EntitySystem
{

    private const List<ProtoId<LibrarianBookDisciplinePrototype>> Disciplines;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        throw new NotImplementedException();
    }
}
