using Content.Shared.Mind;
using Content.Shared.Store.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;


[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingInfectionComponent : Component
{
    [DataField]
    public TimeSpan FirstSymptoms = TimeSpan.Zero;

    [DataField]
    public TimeSpan KnockedOut = TimeSpan.Zero;

    [DataField]
    public TimeSpan FullyInfected = TimeSpan.Zero;

    [DataField]
    public TimeSpan EffectsTimer = TimeSpan.Zero;

    public float EffectsTimerDelay = 20f;

    public float FirstSymptomsDelay = 600f;

    public float KnockedOutDelay = 1200f;

    public float FullyInfectedDelay = 1320f;

    public float ScarySymptomChance = 0.1f;

    public enum InfectionState
    {
        None,
        FirstSymptoms,
        KnockedOut,
        FullyInfected
    }

    public List<string> SymptomMessages = new()
    {
        "changeling-convert-warning-1",
        "changeling-convert-warning-2",
        "changeling-convert-warning-3",
    };

    public List<string> EepyMessages = new()
    {
        "changeling-convert-eeped-1",
        "changeling-convert-eeped-2",
        "changeling-convert-eeped-3",
        "changeling-convert-eeped-4",
    };


    [DataField]
    public InfectionState CurrentState = InfectionState.None;

    // Whether the component has spawned and needs timer setup done
    public bool NeedsInitialization = false;
}
