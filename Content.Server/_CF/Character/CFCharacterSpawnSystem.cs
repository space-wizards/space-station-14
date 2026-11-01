using Content.Shared.Actions;
using Content.Shared.CF.Actions;
using Content.Shared.CF.Character;
using Content.Shared.CF.Shop;
using Content.Shared.CF.Combat;
using Content.Shared.CF.Factions;
using Content.Shared.CF.Preferences;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Overlays;
using Robust.Shared.Prototypes;

namespace Content.Server.CF.Character;

/// <summary>
/// Applies Pathfinder build data from the character profile when a player spawns.
/// </summary>
public sealed partial class CFCharacterSpawnSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private CFFactionSystem _factions = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private CFActionPoolSystem _actionPool = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    /// <summary>Pathfinder Speed of 25 feet is the usual human walk.</summary>
    private const float BaselineSpeedFeet = 25f;

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        ApplyBuild(args.Mob, args.Profile);
        GrantPurse(args.Mob, args.Profile, args.JobId);
        _factions.AssignJob(args.Mob, args.JobId);
    }

    private void GrantPurse(EntityUid mob, Content.Shared.Preferences.HumanoidCharacterProfile profile, string? jobId)
    {
        var purse = EnsureComp<CFPurseComponent>(mob);
        purse.Copper = CFCoins.Remaining(profile, jobId, _prototypes);
        purse.Seeded = true;
        Dirty(mob, purse);
    }

    public void ApplyBuild(EntityUid mob, Content.Shared.Preferences.HumanoidCharacterProfile profile)
    {
        var sheet = CFAttributeMath.Snapshot(_prototypes, profile);

        var abilities = EnsureComp<CFAbilityScoreComponent>(mob);
        abilities.AbilityModifiers = new Dictionary<CFAbility, int>(sheet.Modifiers);
        Dirty(mob, abilities);

        var armorBonus = 0;
        int? dexCap = null;
        var armorProficiency = "Unarmored";
        foreach (var item in _inventory.GetHandOrInventoryEntities(mob))
        {
            if (!TryComp(item, out CFArmorComponent? armor))
                continue;

            // Use the highest item bonus if multiple armor pieces somehow exist.
            if (armor.ItemBonus < armorBonus)
                continue;

            armorBonus = armor.ItemBonus;
            dexCap = armor.DexCap;
            armorProficiency = armor.Proficiency;
        }

        var trainedArmor = armorProficiency == "Unarmored"
            ? CFAttributeMath.IsUnarmoredTrained(sheet.Class)
            : CFAttributeMath.IsArmorTrained(sheet.Class, armorProficiency);

        var combatant = EnsureComp<CFCombatantComponent>(mob);
        combatant.Level = sheet.Level;
        combatant.ArmorClass = CFAttributeMath.ComputeAc(
            sheet.Modifiers,
            sheet.Level,
            trainedArmor,
            armorBonus,
            dexCap);
        combatant.MaxHp = sheet.MaxHp;
        combatant.CurrentHp = sheet.MaxHp;
        combatant.ClassId = profile.CFBuild.Class?.Id;
        combatant.BackgroundId = profile.CFBuild.Background?.Id;
        var ancestryId = sheet.Ancestry?.ID;
        var classId = sheet.Class?.ID;
        var chosenFeats = CFSkills.SelectedFeats(_prototypes, profile.CFBuild, ancestryId, classId);
        var speedBonus = 0;
        combatant.TrainedSkills = CFSkills.CollectTrainedSkills(_prototypes, profile.CFBuild, ancestryId, classId);
        combatant.LoreNames.Clear();
        foreach (var chosen in chosenFeats)
        {
            speedBonus += Math.Max(0, chosen.SpeedBonus);
            foreach (var lore in chosen.Lores)
            {
                if (!combatant.LoreNames.Contains(lore))
                    combatant.LoreNames.Add(lore);
            }
        }
        combatant.TemporaryHp = 0;
        combatant.Dying = 0;
        combatant.Wounded = 0;
        Dirty(mob, combatant);
        CFHeritagePrototype? heritage = null;
        if (profile.CFBuild.Heritage != null)
            _prototypes.TryIndex(profile.CFBuild.Heritage.Value, out heritage);
        if (heritage != null && !heritage.Fits(sheet.Ancestry?.ID))
            heritage = null;
        ApplyAncestry(mob, sheet.Ancestry, heritage, speedBonus);
        _actionPool.Ensure(mob);
        ApplySpellbook(mob, profile, sheet.Class);
        GrantSpellActions(mob, profile);
    }

    /// <summary>
    /// Speed scales the usual walk and sprint. Darkvision uses the same night-vision overlay as goblins.
    /// </summary>
    private void ApplyAncestry(EntityUid mob, CFAncestryPrototype? ancestry, CFHeritagePrototype? heritage, int speedBonus)
    {
        var speed = CFAttributeMath.EffectiveSpeed(ancestry, heritage) + Math.Max(0, speedBonus);
        if (speed > 0 && speed != (int) BaselineSpeedFeet)
        {
            var scale = speed / BaselineSpeedFeet;
            // 2.5 walk, 4.5 sprint, and 20 acceleration are the usual humanoid speeds.
            _speed.ChangeBaseSpeed(mob, 2.5f * scale, 4.5f * scale, 20f);
        }

        if (!CFAttributeMath.HasDarkvision(ancestry, heritage))
            return;

        var vision = EnsureComp<NightVisionComponent>(mob);
        vision.Enabled = true;
        vision.Prioritized = true;
        vision.LightingColor = new Color((byte) 0x9B, (byte) 0xB6, (byte) 0xFF, (byte) 0x30);
        vision.NoiseAmount = 0f;
        Dirty(mob, vision);
    }

    /// <summary>
    /// Known spells become radial actions when a matching <c>CF{spell}</c> entity exists.
    /// </summary>
    private void GrantSpellActions(EntityUid mob, Content.Shared.Preferences.HumanoidCharacterProfile profile)
    {
        var granted = new HashSet<string>();
        foreach (var pick in profile.CFBuild.Spells)
        {
            if (!pick.HasSpell || pick.Rank < 0)
                continue;

            var id = "CF" + pick.Spell.Id;
            if (!granted.Add(id) || !_prototypes.TryIndex<EntityPrototype>(id, out _))
                continue;

            _actions.AddAction(mob, id);
        }
    }

    /// <summary>
    /// Copies class spell slots onto the mob so the HUD can draw pips.
    /// Expended slots come from the character sheet. Cantrips stay in index 0 and are not drawn.
    /// </summary>
    private void ApplySpellbook(EntityUid mob, Content.Shared.Preferences.HumanoidCharacterProfile profile, CFClassPrototype? cls)
    {
        var casting = cls?.Spellcasting;
        if (casting == null)
            return;

        var book = EnsureComp<CFSpellbookComponent>(mob);
        book.Slots.Clear();
        book.Slots.Add(casting.Cantrips);
        book.Slots.AddRange(casting.Slots);

        book.Expended.Clear();
        for (var i = 0; i < book.Slots.Count; i++)
            book.Expended.Add(0);

        foreach (var pick in profile.CFBuild.Spells)
        {
            if (!pick.Expended || !pick.HasSpell || pick.Rank < 0 || pick.Rank >= book.Expended.Count)
                continue;

            book.Expended[pick.Rank]++;
        }

        book.FocusPoints = casting.FocusPoints;
        book.FocusSpent = 0;
        Dirty(mob, book);
    }
}
