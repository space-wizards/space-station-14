using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared._Offbrand.Wounds;

namespace Content.Server._Offbrand.Wounds;

public sealed class ShockGaspThresholdsSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PainSystem _pain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockGaspThresholdsComponent, AfterShockChangeEvent>(OnAfterShockChange);
    }

    private void OnAfterShockChange(Entity<ShockGaspThresholdsComponent> ent, ref AfterShockChangeEvent args)
    {
        var shock = _pain.GetShock(ent.Owner);

        var message = ent.Comp.MessageThresholds.HighestMatch(shock);
        if (message == ent.Comp.CurrentMessage)
            return;

        var previousMessage = ent.Comp.CurrentMessage;

        ent.Comp.CurrentMessage = message;
        Dirty(ent);

        if (previousMessage is { } previous)
        {
            var previousKey = ent.Comp.MessageThresholds.FirstOrDefault(x => x.Value == previous).Key;
            var currentKey = ent.Comp.MessageThresholds.FirstOrDefault(x => x.Value == message).Key;

            if (previousKey >= currentKey)
            {
                return;
            }
        }

        if (message is { } msg)
            _chat.TryEmoteWithChat(ent.Owner, msg, ignoreActionBlocker: true);
    }
}
