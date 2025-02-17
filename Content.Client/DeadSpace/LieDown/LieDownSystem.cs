// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace;
using Content.Shared.DeadSpace.LieDown;
using Robust.Client.Input;

namespace Content.Client.DeadSpace.LieDown;

public sealed class LieDownSystem : SharedLieDownSystem
{
    [Dependency] private readonly IInputManager _input = default!;

    public override void Initialize()
    {
        base.Initialize();

        var common = _input.Contexts.GetContext("human");
        common.AddFunction(DeadSpaceKeys.LieDown);
    }
}
