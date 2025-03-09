using Content.Shared.Magic.Components;

namespace Content.Shared.Magic.Systems;

public abstract class PetrifySpellSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PetrifiedComponent, MapInitEvent>(OnPetrify);
        SubscribeLocalEvent<PetrifiedComponent, AnimateSpellEvent>(OnAnimate);
    }

    protected virtual void OnPetrify(Entity<PetrifiedComponent> ent, ref MapInitEvent args)
    {
        var ev = new PetrifySpellEvent();
        RaiseLocalEvent(ref ev);
    }

    protected virtual void OnAnimate(Entity<PetrifiedComponent> ent, ref AnimateSpellEvent args)
    {

    }
}

[ByRefEvent]
public readonly record struct PetrifySpellEvent;
