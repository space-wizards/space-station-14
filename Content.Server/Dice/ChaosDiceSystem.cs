using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server.Dice;

[RegisterComponent]
public sealed class ChaosDiceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChaosDiceComponent, DiceRollEvent>(OnDiceRoll);
    }

    private void OnDiceRoll(EntityUid uid, ChaosDiceComponent component, ref DiceRollEvent args)
    {
        var ev = new ArtifactActivatedEvent();
        RaiseLocalEvent(uid, ev);
    }
}
