using Content.Shared.Emag.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// This system handles HugBots.
/// </summary>
public abstract class SharedHugBotSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HugBotComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnEmagged(Entity<HugBotComponent> entity, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction) ||
            _emag.CheckFlag(entity, EmagType.Interaction) ||
            !TryComp<HugBotComponent>(entity, out var hugBot))
            return;

        // HugBot HTN checks for emag state within its own logic, so we don't need to change anything here.

        args.Handled = true;
    }
}

/// <summary>
/// This event is raised on an entity when it is hugged by a HugBot.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HugBotHugEvent(NetEntity hugBot) : EntityEventArgs
{
    public readonly NetEntity HugBot = hugBot;
}
