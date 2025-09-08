using Content.Shared._Ronstation.Vampire.Components;
using Content.Shared.Actions;
using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Shared._Ronstation.Vampire.EntitySystems;

public abstract class SharedVampireSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<VampireComponent, FeedEvent>(OnFeedAttempt);
    }

}