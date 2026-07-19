using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Generation;

public sealed partial class GenerationSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _eShuttle = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private IServerDbManager _db = default!;

    /// <summary>
    /// Generation values retrieved from the DB
    /// </summary>
    public Dictionary<string, uint> Generations = new();

    /// <summary>
    /// "Bloodlines" that are present this shift are stored here
    /// </summary>
    public HashSet<string> Present = new();


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChange);
        SubscribeLocalEvent<GenerationComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        LoadFromDb();
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<GenerationComponent> ent, ref MapInitEvent args)
    {
        Present.Add(ent.Comp.DatabaseKey);
        if (Generations.TryGetValue(ent.Comp.DatabaseKey, out var value))
            ent.Comp.GenerationNumber = value;
        else
        {
            // starts at gen 1
            Generations.Add(ent.Comp.DatabaseKey, 1);
            ent.Comp.GenerationNumber = 1;
        }
    }

    private void OnRefreshNameModifiers(Entity<GenerationComponent> ent, ref RefreshNameModifiersEvent args)
    {
        // Don't apply the modifier if the component is being removed
        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (ent.Comp.GenerationNumber == 1 && !ent.Comp.ShowNumberOne)
            return;

        var format = "name-generations";

        // Low priority to stuck it as close as the original name as possible
        args.AddModifier(format, -15, ("roman", ToRomanNumeral(ent.Comp.GenerationNumber)));
    }

    private void OnGameRunLevelChange(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PostRound && ev.Old == GameRunLevel.InRound)
        {
            ShiftEnd();
        }
    }
    private void ShiftEnd()
    {
        // Increase data
        foreach (var key in Present)
        {
            Generations[key] += 1;
        }

        var eShuttle = _eShuttle.GetShuttle();
        var survivors = AllEntityQuery<GenerationComponent>();
        while (survivors.MoveNext(out var ent, out var generation))
        {
            if (_mobState.IsDead(ent))
            {
                continue;
            }

            if (eShuttle != null && eShuttle.Value.IsValid() && generation.MustEvac)
            {
                if (Transform(eShuttle.Value).MapID != _xform.GetMapCoordinates(ent).MapId)
                {
                    // not on evac...
                    continue;
                }

            }

            // we lived, reset generation counter!
            Generations[generation.DatabaseKey] = 1;
        }

        Present.Clear();

        // Store data
        StoreIntoDb();
    }

    private async void LoadFromDb()
    {
        await foreach (var keypair in _db.LoadGenerations())
        {
            Generations.Add(keypair.Item1, keypair.Item2);
        }
    }

    private async void StoreIntoDb()
    {
        await _db.SaveGenerations(Generations);
    }

    private static readonly List<(uint, string)> Map = [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];

    /// <summary>
    /// Converts an unsigned integer to roman numeral form (e.g. 7 to VII)
    /// </summary>
    public string ToRomanNumeral(uint num)
    {
        // from aziz alto's answer at https://stackoverflow.com/questions/28777219/basic-program-to-convert-integer-to-roman-numerals
        var text = "";
        var value = num;

        while (value > 0)
        {
            foreach (var keypair in Map)
            {
                while (value >= keypair.Item1)
                {
                    value -= keypair.Item1;
                    text += keypair.Item2;
                }
            }
        }
        return text;
    }
}
