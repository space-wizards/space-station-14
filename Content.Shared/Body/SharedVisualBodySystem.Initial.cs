using Content.Shared.Humanoid;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    private void InitializeInitial()
    {
        SubscribeLocalEvent<VisualBodyComponent, MapInitEvent>(OnVisualMapInit, after: [typeof(InitialBodySystem)]);
    }

    private void OnVisualMapInit(Entity<VisualBodyComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoidProfile))
            return;

        ApplyAppearanceTo(ent.AsNullable(), HumanoidCharacterAppearance.DefaultWithSpecies(humanoidProfile.Species, humanoidProfile.Sex), humanoidProfile.Sex);
    }
}
