using System.Linq;
using Content.Server.Chat.Systems;
using Content.Shared._Offbrand.Wounds;

namespace Content.Server._Offbrand.Wounds;

public sealed class BrainGaspThresholdsSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainGaspThresholdsComponent, AfterBrainDamageChanged>(OnAfterBrainDamageChanged);
        SubscribeLocalEvent<BrainGaspThresholdsComponent, AfterBrainOxygenChanged>(OnAfterBrainOxygenChanged);
    }

    private void OnAfterBrainDamageChanged(Entity<BrainGaspThresholdsComponent> ent, ref AfterBrainDamageChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        var message = ent.Comp.MessageThresholds.HighestMatch(brain.Damage);
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

    private void OnAfterBrainOxygenChanged(Entity<BrainGaspThresholdsComponent> ent, ref AfterBrainOxygenChanged args)
    {
        var brain = Comp<BrainDamageComponent>(ent);

        var message = ent.Comp.OxygenThresholds.LowestMatch(brain.Oxygen);
        if (message == ent.Comp.CurrentOxygen)
            return;

        var previousMessage = ent.Comp.CurrentOxygen;

        ent.Comp.CurrentOxygen = message;
        Dirty(ent);

        if (previousMessage is { } previous)
        {
            var previousKey = ent.Comp.OxygenThresholds.FirstOrDefault(x => x.Value == previous).Key;
            var currentKey = ent.Comp.OxygenThresholds.FirstOrDefault(x => x.Value == message).Key;

            if (previousKey <= currentKey)
            {
                return;
            }
        }

        if (message is { } msg)
            _chat.TryEmoteWithChat(ent.Owner, msg, ignoreActionBlocker: true);
    }
}
