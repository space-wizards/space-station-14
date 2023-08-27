using Content.Client.CharacterInfo;
using Content.Shared.Points;
using Robust.Shared.GameStates;

namespace Content.Client.Points;

/// <inheritdoc/>
public sealed class PointSystem : SharedPointSystem
{
    [Dependency] private readonly CharacterInfoSystem _characterInfo = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PointManagerComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, PointManagerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not PointManagerComponentState state)
            return;

        component.Points = new(state.Points);
        component.Scoreboard = state.Scoreboard;
        _characterInfo.RequestCharacterInfo();
    }
}
