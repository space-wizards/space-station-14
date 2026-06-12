using Content.Shared.Body;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._Offbrand.Medical;

public sealed partial class StethoscopeDescriptionSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, StethoscopeExamineEvent>(_statusEffects.RelayEvent);
        SubscribeLocalEvent<StethoscopeDescriptionComponent, StethoscopeExamineEvent>(OnStethoscopeExamine);
        SubscribeLocalEvent<StethoscopeDescriptionComponent, StatusEffectRelayedEvent<StethoscopeExamineEvent>>(OnRelayedStethoscopeExamine);
    }

    private void OnStethoscopeExamine(Entity<StethoscopeDescriptionComponent> ent, ref StethoscopeExamineEvent args)
    {
        AddDescription(ent, ref args);
    }

    private void OnRelayedStethoscopeExamine(Entity<StethoscopeDescriptionComponent> ent,
        ref StatusEffectRelayedEvent<StethoscopeExamineEvent> args)
    {
        var ev = args.Args;
        AddDescription(ent, ref ev);
        args.Args = ev;
    }

    private void AddDescription(Entity<StethoscopeDescriptionComponent> ent, ref StethoscopeExamineEvent args)
    {
        args.Messages.Add(Loc.GetString(ent.Comp.Description));
    }
}
