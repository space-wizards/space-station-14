using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;

namespace Content.Shared.Genetics;

[Prototype]
public sealed partial class GeneticMutationPrototype : IPrototype
{
    public static char GetComplement(char nucleotide)
    {
        return char.ToUpperInvariant(nucleotide) switch
        {
            'A' => 'T',
            'T' => 'A',
            'G' => 'C',
            'C' => 'G',
            _ => throw new ArgumentOutOfRangeException(nameof(nucleotide), nucleotide, "Expected a DNA nucleotide."),
        };
    }

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; } = string.Empty;

    [DataField(required: true)]
    public LocId Description { get; private set; } = string.Empty;

    [DataField(required: true)]
    public string Sequence { get; private set; } = string.Empty;

    [DataField]
    public int Instability;

    [DataField]
    public float WalkSpeedModifier = 1f;

    [DataField]
    public float SprintSpeedModifier = 1f;

    [DataField]
    public float MeleeBruteBonus = 0f;

    [DataField]
    public bool CanBreakWalls = false;

    [DataField]
    public bool GrantsXRay = false;

    [DataField]
    public bool GrantsAdaptiveVision = false;

    [DataField]
    public float TelekinesisRange = 0f;

    [DataField]
    public float RegenerationBrute = 0f;

    [DataField]
    public float RegenerationBurn = 0f;

    [DataField]
    public float RegenerationInterval = 10f;

    [DataField]
    public bool GrantsTelepathy = false;

    [DataField]
    public bool GrantsShockTouch;

    [DataField]
    public bool GrantsLaserEyes;

    [DataField]
    public bool GrantsPressureImmunity;

    [DataField]
    public bool GrantsRadiation;

    [DataField]
    public float FireResistance = 0f;

    [DataField]
    public float ColdResistance = 0f;

    [DataField]
    public float RadiationResistance = 0f;

    [DataField]
    public float BruteDamageModifier = 1f;

    [DataField]
    public float ScaleModifier = 0f;

    [DataField]
    public float LightRadius = 0f;

    [DataField]
    public float LightEnergy = 0f;

    [DataField]
    public bool GrantsChameleon = false;

    [DataField]
    public bool GrantsSpeciesShift = false;

    [DataField]
    public bool GrantsPlasmaBreathing = false;

    [DataField]
    public bool GrantsNoBreathing = false;

    [DataField]
    public bool GrantsAnimalHearing = false;

    [DataField]
    public float EpilepsyMinInterval = 0f;

    [DataField]
    public float EpilepsyMaxInterval = 0f;

    [DataField]
    public float EpilepsyStunDuration = 0f;

    [DataField]
    public float TouretteMinInterval = 0f;

    [DataField]
    public float TouretteMaxInterval = 0f;

    [DataField]
    public List<string> TouretteExclamations = new();

    [DataField]
    public float NarcolepsyMinInterval = 0f;

    [DataField]
    public float NarcolepsyMaxInterval = 0f;

    [DataField]
    public float NarcolepsySleepDuration = 0f;

    [DataField]
    public float BruteDamageMultiplier = 1f;

    [DataField]
    public float ParanoiaMinInterval = 0f;

    [DataField]
    public float ParanoiaMaxInterval = 0f;

    [DataField]
    public bool GrantsClumsiness = false;

    [DataField]
    public bool GrantsRandomNoises = false;

    [DataField]
    public float NoisesMinInterval = 0f;

    [DataField]
    public float NoisesMaxInterval = 0f;

    [DataField]
    public bool GrantsBlindness;

    [DataField]
    public bool GrantsAntiGlow;

    [DataField]
    public bool GrantsMute;

    [DataField]
    public bool GrantsDeafness;

    [DataField]
    public bool GrantsInsulation;

    [DataField]
    public bool GrantsIlliteracy;

    [DataField]
    public bool GrantsStuttering;

    [DataField]
    public bool GrantsMobsterAccent;

    [DataField]
    public bool GrantsAllCapsAccent;

    [DataField]
    public bool GrantsScrambledAccent;

    [DataField]
    public bool GrantsOwOAccent;

    [DataField]
    public bool GrantsMonkeyAccent;

    [DataField]
    public bool GrantsMothAccent;

    [DataField]
    public bool GrantsBackwardsAccent;

    [DataField]
    public bool GrantsNearsighted;

    [DataField]
    public bool GrantsCough;

    [DataField]
    public bool GrantsTwoLeftFeet;

    [DataField]
    public float MinorNegativeMinInterval;

    [DataField]
    public float MinorNegativeMaxInterval;

    [DataField]
    public float MinorNegativeDuration;
}
