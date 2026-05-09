namespace Content.Server._FinalStand.GameTicking.Rules;


/// raised broadcast on the server when a wave's combat phase ends (all enemies dead or fallback timer expired).

[ByRefEvent]
public readonly record struct WaveEndedEvent(int WaveNumber);
