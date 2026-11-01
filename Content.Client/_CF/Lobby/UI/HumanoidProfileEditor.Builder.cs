using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Client.CF.Lobby.UI.CharacterBuilder;
using Content.Shared.CF.Character;
using Content.Shared.CF.Preferences;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private const int DetailsTab = 0;
    private const int ShopTabIndex = 1;
    private const int WeaponsTabIndex = 2;
    private const int DefenseTabIndex = 3;
    private const int GearTabIndex = 4;
    private const int SpellsTabIndex = 5;

    private enum BuilderSelection
    {
        Ancestry,
        Background,
        Class,
        Job,
        Heritage,
        Sprite,
        AncestryFeat,
        ClassFeat,
        ClassFeatLevel2,
    }

    private enum SkillAssignMode
    {
        Background,
        Class,
    }

    private BuilderSelection _selection = BuilderSelection.Ancestry;
    private SkillAssignMode _skillMode;
    private bool _pickerOpen;
    private string? _pendingId;
    private string? _prdUrl;

    private const string AncestryPrd = "https://2e.aonprd.com/Ancestries.aspx";
    private const string BackgroundPrd = "https://2e.aonprd.com/Backgrounds.aspx";
    private const string ClassPrd = "https://2e.aonprd.com/Classes.aspx";

    private static readonly CFAbility[] AllAbilities = Enum.GetValues<CFAbility>();

    private void SetupCharacterBuilder()
    {
        var group = new ButtonGroup();
        AncestryButton.Group = group;
        BackgroundButton.Group = group;
        ClassButton.Group = group;
        JobButton.Group = group;

        AncestryButton.OnPressed += _ => ShowAncestrySelection();
        BackgroundButton.OnPressed += _ => ShowBackgroundSelection();
        ClassButton.OnPressed += _ => ShowClassSelection();
        JobButton.OnPressed += _ => ShowJobSelection();
        JobCloseButton.OnPressed += _ => ClosePicker();
        JobAcceptButton.OnPressed += _ => ClosePicker();
        JobCancelButton.OnPressed += _ => ClosePicker();

        SelectionView.OnEntrySelected += id => _pendingId = id;
        SelectionView.OnAccept += AcceptPicker;
        SelectionView.OnClosed += ClosePicker;
        SelectionView.OnPrdPressed += OpenPrd;

        BoostView.OnClosed += ClosePicker;
        BoostView.OnAccept += AcceptBoostAssignment;
        SetAbilitiesButton.OnPressed += _ => ShowBoostAssignment();
        HeritageSlot.OnPressed += _ => ShowHeritageSelection();
        AncestryFeatSlot.OnPressed += _ => ShowAncestryFeatSelection();
        ClassFeatSlot.OnPressed += _ => ShowClassFeatSelection(1);
        Level2FeatSlot.OnPressed += _ => ShowClassFeatSelection(2);
        SpriteSlot.OnPressed += _ => ShowSpriteSelection();

        SkillView.OnClosed += ClosePicker;
        SkillView.OnAccept += AcceptSkillAssignment;
        SkillTrainingButton.OnPressed += _ => ShowSkillTraining();
        ClassSkillButton.OnPressed += _ => ShowClassSkill();

        TabContainer.SetTabTitle(DetailsTab, Loc.GetString("cf-character-builder-details-tab"));
        TabContainer.SetTabTitle(ShopTabIndex, Loc.GetString("cf-character-builder-shop-tab"));
        TabContainer.SetTabTitle(WeaponsTabIndex, Loc.GetString("cf-character-builder-weapons-tab"));
        TabContainer.SetTabTitle(DefenseTabIndex, Loc.GetString("cf-character-builder-defense-tab"));
        TabContainer.SetTabTitle(GearTabIndex, Loc.GetString("cf-character-builder-gear-tab"));
        TabContainer.SetTabTitle(SpellsTabIndex, Loc.GetString("cf-character-builder-spells-tab"));

        UpdateBuilderSummary();
        Level2Toggle.OnPressed += _ => Level2Body.Visible = Level2Toggle.Pressed;
        Level3Toggle.OnPressed += _ => Level3Body.Visible = Level3Toggle.Pressed;
    }

    private CFCharacterBuildData CurrentBuild()
    {
        return Profile?.CFBuild.Clone() ?? new CFCharacterBuildData();
    }

    private void SetBuild(CFCharacterBuildData build)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithCFBuild(build);
        SetDirty();
        UpdateBuilderSummary();
    }

    private void AcceptPicker()
    {
        var id = _pendingId;
        ClosePicker();
        if (id != null)
            OnBuilderOptionSelected(id);
    }

    private void ClosePicker()
    {
        _pickerOpen = false;
        _pendingId = null;
        SelectionView.Visible = false;
        BoostView.Visible = false;
        SkillView.Visible = false;
        JobPane.Visible = false;
        EditorPane.Visible = true;
        AncestryButton.Pressed = false;
        BackgroundButton.Pressed = false;
        ClassButton.Pressed = false;
        JobButton.Pressed = false;
        HeritageSlot.Pressed = false;
        SpriteSlot.Pressed = false;
        AncestryFeatSlot.Pressed = false;
        ClassFeatSlot.Pressed = false;
        Level2FeatSlot.Pressed = false;
    }

    private void OpenPicker()
    {
        EditorPane.Visible = false;
        JobPane.Visible = false;
        BoostView.Visible = false;
        SkillView.Visible = false;
        SelectionView.Visible = true;
        _pickerOpen = true;
    }

    private void OpenBoostView()
    {
        EditorPane.Visible = false;
        JobPane.Visible = false;
        SelectionView.Visible = false;
        SkillView.Visible = false;
        BoostView.Visible = true;
        _pickerOpen = true;
    }

    private void OpenSkillView()
    {
        EditorPane.Visible = false;
        JobPane.Visible = false;
        SelectionView.Visible = false;
        BoostView.Visible = false;
        SkillView.Visible = true;
        _pickerOpen = true;
    }

    private void ShowJobSelection()
    {
        _selection = BuilderSelection.Job;
        _pickerOpen = false;
        EditorPane.Visible = false;
        SelectionView.Visible = false;
        BoostView.Visible = false;
        SkillView.Visible = false;
        JobPane.Visible = true;
        RefreshJobs();
    }

    private void OpenPrd()
    {
        if (string.IsNullOrWhiteSpace(_prdUrl))
            return;

        IoCManager.Resolve<IUriOpener>().OpenUri(new Uri(_prdUrl));
    }

    private void OnBuilderOptionSelected(string id)
    {
        switch (_selection)
        {
            case BuilderSelection.Ancestry:
            {
                var current = Profile == null ? null : CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
                var picked = CFAttributeMath.FindAncestry(_prototypeManager, id);
                if (picked == null)
                    SetSpecies(id);
                else if (current?.ID != picked.ID)
                    SetSpecies(picked.Species);

                var build = CurrentBuild();
                if (current?.ID != picked?.ID)
                    build.AncestryFreeBoosts.Clear();
                if (build.Heritage != null
                    && _prototypeManager.TryIndex(build.Heritage.Value, out CFHeritagePrototype? heritage)
                    && !heritage.Fits(picked?.ID))
                    build.Heritage = null;
                if (build.AncestryFeat != null
                    && _prototypeManager.TryIndex(build.AncestryFeat.Value, out CFFeatPrototype? feat)
                    && !feat.Fits(picked?.ID, build.Level))
                    build.AncestryFeat = null;
                SetBuild(build);
                break;
            }
            case BuilderSelection.Sprite:
                SetSpecies(id);
                break;
            case BuilderSelection.Background:
            {
                var next = CurrentBuild();
                next.Background = id;
                next.BackgroundBoost = null;
                next.BackgroundFreeBoost = null;
                next.BackgroundTrainedSkills.Clear();
                SetBuild(next);
                break;
            }
            case BuilderSelection.AncestryFeat:
            {
                var next = CurrentBuild();
                next.AncestryFeat = id;
                SetBuild(next);
                break;
            }
            case BuilderSelection.Heritage:
            {
                var next = CurrentBuild();
                next.Heritage = id;
                SetBuild(next);
                if (Profile != null
                    && CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species) is { } ancestry
                    && ancestry.Sprites.FirstOrDefault(sprite => sprite.Species == Profile.Species) is { } current
                    && current.Heritage is { } required
                    && required.Id != id)
                    SetSpecies(ancestry.Species);
                break;
            }
            case BuilderSelection.Class:
            {
                var next = CurrentBuild();
                next.Class = id;
                next.ClassTrainedSkills.Clear();
                if (next.ClassFeat != null
                    && _prototypeManager.TryIndex(next.ClassFeat.Value, out CFFeatPrototype? classFeat)
                    && !classFeat.FitsClass(id, 1))
                    next.ClassFeat = null;
                if (next.ClassFeatLevel2 != null
                    && _prototypeManager.TryIndex(next.ClassFeatLevel2.Value, out CFFeatPrototype? classFeatLevel2)
                    && !classFeatLevel2.FitsClass(id, 2))
                    next.ClassFeatLevel2 = null;
                SetBuild(next);
                break;
            }
            case BuilderSelection.ClassFeat:
            {
                var next = CurrentBuild();
                next.ClassFeat = id;
                if (next.ClassFeatLevel2?.Id == id)
                    next.ClassFeatLevel2 = null;
                SetBuild(next);
                break;
            }
            case BuilderSelection.ClassFeatLevel2:
            {
                var next = CurrentBuild();
                next.ClassFeatLevel2 = id;
                if (next.ClassFeat?.Id == id)
                    next.ClassFeat = null;
                SetBuild(next);
                break;
            }
        }
    }

    private void ShowAncestrySelection()
    {
        _selection = BuilderSelection.Ancestry;
        _prdUrl = AncestryPrd;
        var currentAncestry = Profile == null ? null : CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        var selected = currentAncestry?.Species.Id ?? Profile?.Species.Id;
        _pendingId = selected;
        var covered = new HashSet<string>();
        var options = new List<Pf2eListEntry>();

        foreach (var ancestry in _prototypeManager.EnumeratePrototypes<CFAncestryPrototype>().OrderBy(ancestry => Loc.GetString(ancestry.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            covered.Add(ancestry.Species);
            foreach (var sprite in ancestry.Sprites)
                covered.Add(sprite.Species);
            options.Add(new Pf2eListEntry
            {
                Id = ancestry.Species,
                Name = Loc.GetString(ancestry.Name),
                Rarity = ancestry.Rarity,
                Traits = JoinLoc(ancestry.Traits),
                Description = Loc.GetString(ancestry.Description),
                HitPoints = Stat("cf-character-builder-stat-hp", ancestry.HitPoints.ToString()),
                Size = Stat("cf-character-builder-stat-size", Loc.GetString($"cf-size-{ancestry.Size.ToString().ToLower()}")),
                Speed = Stat("cf-character-builder-stat-speed", $"{ancestry.Speed} {Loc.GetString("cf-character-builder-feet")}"),
                Boosts = Stat("cf-character-builder-stat-boosts", FormatBoosts(ancestry.AbilityBoosts)),
                Flaws = Stat("cf-character-builder-stat-flaws", FormatAbilities(ancestry.AbilityFlaws)),
                Languages = Stat("cf-character-builder-stat-languages", JoinLoc(ancestry.Languages)),
            });
        }

        foreach (var species in _prototypeManager.EnumeratePrototypes<SpeciesPrototype>().Where(species => species.RoundStart && !covered.Contains(species.ID)).OrderBy(species => Loc.GetString(species.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            var none = Loc.GetString("cf-character-builder-none");
            options.Add(new Pf2eListEntry
            {
                Id = species.ID,
                Name = Loc.GetString(species.Name),
                Rarity = CFRarity.Uncommon,
                Traits = Loc.GetString(species.Name),
                Description = "",
                HitPoints = Stat("cf-character-builder-stat-hp", none),
                Size = Stat("cf-character-builder-stat-size", none),
                Speed = Stat("cf-character-builder-stat-speed", none),
                Boosts = Stat("cf-character-builder-stat-boosts", none),
                Flaws = Stat("cf-character-builder-stat-flaws", none),
                Languages = Stat("cf-character-builder-stat-languages", none),
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-ancestry"),
            Loc.GetString("cf-character-builder-filter-all-ancestries"),
            options,
            selected);
        OpenPicker();
    }

    private void ShowBackgroundSelection()
    {
        _selection = BuilderSelection.Background;
        _prdUrl = BackgroundPrd;
        var selected = Profile?.CFBuild.Background?.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        foreach (var background in _prototypeManager.EnumeratePrototypes<CFBackgroundPrototype>().OrderBy(background => Loc.GetString(background.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            options.Add(new Pf2eListEntry
            {
                Id = background.ID,
                Name = Loc.GetString(background.Name),
                Rarity = background.Rarity,
                Traits = Loc.GetString("cf-character-builder-skills", ("skills", FormatSkills(background.SkillBoosts, background.TrainedSkills))),
                Description = Loc.GetString(background.Description),
                Boosts = Stat("cf-character-builder-stat-boosts", FormatBoosts(background.AbilityBoosts)),
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-background"),
            Loc.GetString("cf-character-builder-filter-all-backgrounds"),
            options,
            selected);
        OpenPicker();
    }

    private void ShowClassSelection()
    {
        _selection = BuilderSelection.Class;
        _prdUrl = ClassPrd;
        var selected = Profile?.CFBuild.Class?.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        foreach (var cls in _prototypeManager.EnumeratePrototypes<CFClassPrototype>().OrderBy(cls => Loc.GetString(cls.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            options.Add(new Pf2eListEntry
            {
                Id = cls.ID,
                Name = Loc.GetString(cls.Name),
                Rarity = cls.Rarity,
                Traits = Loc.GetString("cf-character-builder-class-key-ability", ("ability", AbilityName(cls.KeyAbility))),
                Description = DescribeClass(cls),
                HitPoints = Stat("cf-character-builder-stat-hp", cls.HitPoints.ToString()),
                Boosts = Stat("cf-character-builder-stat-boosts", AbilityName(cls.KeyAbility)),
                Rules = string.Join('\n', new[]
                {
                    Loc.GetString("cf-character-builder-class-hp-ability", ("ability", AbilityName(cls.HitPointsAbility))),
                    DescribeProficiencies(cls.Proficiencies),
                }.Where(line => !string.IsNullOrWhiteSpace(line))),
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-class"),
            Loc.GetString("cf-character-builder-filter-all-classes"),
            options,
            selected);
        OpenPicker();
    }

    private static string DescribeClass(CFClassPrototype cls)
    {
        var text = Loc.GetString(cls.Description);
        if (cls.Features.Count == 0)
            return text;

        var lines = new List<string> { text };
        foreach (var feature in cls.Features.OrderBy(feature => feature.Level).ThenBy(feature => feature.Name, StringComparer.Ordinal))
            lines.Add($"Level {feature.Level}: {feature.Name}. {feature.Description}");

        return string.Join("\n", lines);
    }

    private void ShowAncestryFeatSelection()
    {
        if (Profile == null)
            return;

        _selection = BuilderSelection.AncestryFeat;
        _prdUrl = AncestryPrd;
        AncestryButton.Pressed = false;
        BackgroundButton.Pressed = false;
        ClassButton.Pressed = false;
        JobButton.Pressed = false;
        HeritageSlot.Pressed = false;
        SpriteSlot.Pressed = false;
        AncestryFeatSlot.Pressed = true;
        ClassFeatSlot.Pressed = false;
        Level2FeatSlot.Pressed = false;

        var build = CurrentBuild();
        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        var selected = build.AncestryFeat?.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        foreach (var feat in _prototypeManager.EnumeratePrototypes<CFFeatPrototype>()
                     .Where(feat => ancestry != null && feat.Fits(ancestry.ID, build.Level))
                     .OrderBy(feat => feat.Level)
                     .ThenBy(feat => Loc.GetString(feat.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            options.Add(new Pf2eListEntry
            {
                Id = feat.ID,
                Name = Loc.GetString(feat.Name),
                Rarity = feat.Rarity,
                Traits = Loc.GetString("cf-character-builder-feat-level", ("level", feat.Level)),
                Description = Loc.GetString(feat.Description),
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-ancestry-feat"),
            Loc.GetString("cf-character-builder-filter-all-ancestry-feats"),
            options,
            selected);
        OpenPicker();
    }

    private void ShowClassFeatSelection(int maxLevel)
    {
        if (Profile == null)
            return;

        _selection = maxLevel <= 1 ? BuilderSelection.ClassFeat : BuilderSelection.ClassFeatLevel2;
        _prdUrl = AncestryPrd;
        AncestryButton.Pressed = false;
        BackgroundButton.Pressed = false;
        ClassButton.Pressed = false;
        JobButton.Pressed = false;
        HeritageSlot.Pressed = false;
        SpriteSlot.Pressed = false;
        AncestryFeatSlot.Pressed = false;
        ClassFeatSlot.Pressed = maxLevel <= 1;
        Level2FeatSlot.Pressed = maxLevel > 1;

        var build = CurrentBuild();
        var classId = build.Class?.Id;
        var selected = maxLevel <= 1 ? build.ClassFeat?.Id : build.ClassFeatLevel2?.Id;
        var other = maxLevel <= 1 ? build.ClassFeatLevel2?.Id : build.ClassFeat?.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        foreach (var feat in _prototypeManager.EnumeratePrototypes<CFFeatPrototype>()
                     .Where(feat => feat.FitsClass(classId, maxLevel) && feat.ID != other)
                     .OrderBy(feat => feat.Level)
                     .ThenBy(feat => Loc.GetString(feat.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            options.Add(new Pf2eListEntry
            {
                Id = feat.ID,
                Name = Loc.GetString(feat.Name),
                Rarity = feat.Rarity,
                Traits = Loc.GetString("cf-character-builder-feat-level", ("level", feat.Level)),
                Description = Loc.GetString(feat.Description),
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-class-feat"),
            Loc.GetString("cf-character-builder-filter-all-class-feats"),
            options,
            selected);
        OpenPicker();
    }

    private void ShowHeritageSelection()
    {
        if (Profile == null)
            return;

        _selection = BuilderSelection.Heritage;
        _prdUrl = AncestryPrd;
        AncestryButton.Pressed = false;
        BackgroundButton.Pressed = false;
        ClassButton.Pressed = false;
        JobButton.Pressed = false;
        HeritageSlot.Pressed = true;
        SpriteSlot.Pressed = false;
        AncestryFeatSlot.Pressed = false;
        ClassFeatSlot.Pressed = false;
        Level2FeatSlot.Pressed = false;

        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        var selected = Profile.CFBuild.Heritage?.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        foreach (var heritage in _prototypeManager.EnumeratePrototypes<CFHeritagePrototype>()
                     .Where(heritage => heritage.Fits(ancestry?.ID))
                     .OrderBy(heritage => Loc.GetString(heritage.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            var traits = heritage.Ancestries.Count == 0
                ? Loc.GetString("cf-character-builder-heritage-versatile")
                : Loc.GetString(ancestry?.Name ?? "cf-character-builder-none");
            options.Add(new Pf2eListEntry
            {
                Id = heritage.ID,
                Name = Loc.GetString(heritage.Name),
                Rarity = heritage.Rarity,
                Traits = traits,
                Description = Loc.GetString(heritage.Description),
                HitPoints = heritage.HitPoints > 0 ? Stat("cf-character-builder-stat-hp", heritage.HitPoints.ToString()) : null,
                Size = heritage.Size is { } size
                    ? Stat("cf-character-builder-stat-size", Loc.GetString($"cf-size-{size.ToString().ToLower()}"))
                    : null,
                Speed = heritage.Speed > 0
                    ? Stat("cf-character-builder-stat-speed", $"{heritage.Speed} {Loc.GetString("cf-character-builder-feet")}")
                    : null,
            });
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-heritage"),
            Loc.GetString("cf-character-builder-filter-all-heritages"),
            options,
            selected);
        OpenPicker();
    }

    private void ShowSpriteSelection()
    {
        if (Profile == null)
            return;

        _selection = BuilderSelection.Sprite;
        _prdUrl = AncestryPrd;
        AncestryButton.Pressed = false;
        BackgroundButton.Pressed = false;
        ClassButton.Pressed = false;
        JobButton.Pressed = false;
        HeritageSlot.Pressed = false;
        SpriteSlot.Pressed = true;
        AncestryFeatSlot.Pressed = false;
        ClassFeatSlot.Pressed = false;
        Level2FeatSlot.Pressed = false;

        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        var selected = Profile.Species.Id;
        _pendingId = selected;
        var options = new List<Pf2eListEntry>();

        if (ancestry != null)
        {
            var defaultName = ancestry.Species.Id is "Human" or "Dwarf"
                ? Loc.GetString(_prototypeManager.Index<SpeciesPrototype>(ancestry.Species).Name)
                : Loc.GetString("cf-sprite-humanoid");
            options.Add(new Pf2eListEntry
            {
                Id = ancestry.Species,
                Name = defaultName,
                Rarity = CFRarity.Common,
                Traits = Loc.GetString(ancestry.Name),
                Description = WithSpriteCredits(Loc.GetString("cf-sprite-humanoid-desc"), "Mobs/Species/Human/parts.rsi"),
            });

            var heritageId = Profile.CFBuild.Heritage?.Id;
            foreach (var sprite in ancestry.Sprites.OrderBy(sprite => Loc.GetString(sprite.Name), StringComparer.CurrentCultureIgnoreCase))
            {
                if (sprite.Heritage is { } required && required.Id != heritageId)
                    continue;

                options.Add(new Pf2eListEntry
                {
                    Id = sprite.Species,
                    Name = Loc.GetString(sprite.Name),
                    Rarity = CFRarity.Common,
                    Traits = Loc.GetString(ancestry.Name),
                    Description = WithSpriteCredits(Loc.GetString(sprite.Description), sprite.Parts),
                });
            }
        }

        SelectionView.Show(
            Loc.GetString("cf-character-builder-title-sprite"),
            Loc.GetString("cf-character-builder-filter-all-sprites"),
            options,
            selected);
        OpenPicker();
    }

    /// <summary>Appends the parts RSI license and copyright so the picker shows who drew the sprite.</summary>
    private string WithSpriteCredits(string description, string? rsiPath)
    {
        var credits = ReadSpriteCredits(rsiPath);
        return string.IsNullOrEmpty(credits) ? description : $"{description}\n\n{credits}";
    }

    private string? ReadSpriteCredits(string? rsiPath)
    {
        if (string.IsNullOrWhiteSpace(rsiPath))
            return null;

        var path = new ResPath($"/Textures/{rsiPath}/meta.json");
        if (!_resManager.TryContentFileRead(path, out var stream))
            return null;

        using (stream)
        using (var reader = new StreamReader(stream))
        {
            var text = reader.ReadToEnd();
            var license = MatchMeta(text, "license");
            var copyright = MatchMeta(text, "copyright");
            if (license == null && copyright == null)
                return null;

            return license != null && copyright != null ? $"{license}\n{copyright}" : license ?? copyright;
        }
    }

    private static string? MatchMeta(string text, string key)
    {
        var match = Regex.Match(text, $"\"{key}\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"");
        return match.Success ? Regex.Unescape(match.Groups["value"].Value) : null;
    }

    private void ShowBoostAssignment()
    {
        if (Profile == null)
            return;

        var build = CurrentBuild();
        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        var slots = new List<BoostSlotDefinition>();

        if (ancestry != null)
        {
            var freeCount = CFAttributeMath.CountAncestryFreeBoosts(ancestry);
            var ancestryBlocked = new HashSet<CFAbility>();
            foreach (var boost in ancestry.AbilityBoosts)
            {
                if (boost != CFAbilityBoost.Free)
                    ancestryBlocked.Add((CFAbility) boost);
            }

            foreach (var flaw in ancestry.AbilityFlaws)
                ancestryBlocked.Add(flaw);

            var ancestryAllowed = AllAbilities.Where(ability => !ancestryBlocked.Contains(ability)).ToList();
            for (var i = 0; i < freeCount; i++)
            {
                CFAbility? selected = i < build.AncestryFreeBoosts.Count ? build.AncestryFreeBoosts[i] : null;
                slots.Add(new BoostSlotDefinition
                {
                    Id = $"ancestry-free-{i}",
                    Group = "ancestry",
                    Label = Loc.GetString("cf-character-builder-boost-ancestry-free", ("index", i + 1)),
                    Allowed = ancestryAllowed,
                    Selected = selected,
                });
            }
        }

        if (build.Background != null
            && _prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
        {
            var choices = CFAttributeMath.BackgroundBoostChoices(background);
            if (choices.Count > 0)
            {
                slots.Add(new BoostSlotDefinition
                {
                    Id = "background-choice",
                    Group = "background",
                    Label = Loc.GetString("cf-character-builder-boost-background-choice"),
                    Allowed = choices,
                    Selected = build.BackgroundBoost,
                });
            }

            slots.Add(new BoostSlotDefinition
            {
                Id = "background-free",
                Group = "background",
                Label = Loc.GetString("cf-character-builder-boost-background-free"),
                Allowed = AllAbilities.ToList(),
                Selected = build.BackgroundFreeBoost,
            });
        }

        for (var i = 0; i < CFAttributeMath.FinishingFreeBoostCount; i++)
        {
            CFAbility? selected = i < build.FreeBoosts.Count ? build.FreeBoosts[i] : null;
            slots.Add(new BoostSlotDefinition
            {
                Id = $"free-{i}",
                Group = "finishing",
                Label = Loc.GetString("cf-character-builder-boost-finishing", ("index", i + 1)),
                Allowed = AllAbilities.ToList(),
                Selected = selected,
            });
        }

        if (slots.Count == 0)
        {
            // Nothing to assign yet — still show empty guidance.
            BoostView.Show(
                Loc.GetString("cf-character-builder-set-abilities"),
                Loc.GetString("cf-character-builder-boost-need-abc"),
                Array.Empty<BoostSlotDefinition>());
            OpenBoostView();
            return;
        }

        BoostView.Show(
            Loc.GetString("cf-character-builder-set-abilities"),
            Loc.GetString("cf-character-builder-boost-help"),
            slots);
        OpenBoostView();
    }

    private void AcceptBoostAssignment()
    {
        if (Profile == null)
            return;

        var selections = BoostView.GetSelections();
        var build = CurrentBuild();
        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);

        build.AncestryFreeBoosts.Clear();
        if (ancestry != null)
        {
            var freeCount = CFAttributeMath.CountAncestryFreeBoosts(ancestry);
            var picked = new HashSet<CFAbility>();
            for (var i = 0; i < freeCount; i++)
            {
                if (!selections.TryGetValue($"ancestry-free-{i}", out var ability) || ability == null)
                    continue;

                if (!picked.Add(ability.Value))
                    continue;

                build.AncestryFreeBoosts.Add(ability.Value);
            }
        }

        if (selections.TryGetValue("background-choice", out var bgChoice))
            build.BackgroundBoost = bgChoice;

        if (selections.TryGetValue("background-free", out var bgFree))
            build.BackgroundFreeBoost = bgFree;

        build.FreeBoosts.Clear();
        var finishingPicked = new HashSet<CFAbility>();
        for (var i = 0; i < CFAttributeMath.FinishingFreeBoostCount; i++)
        {
            if (!selections.TryGetValue($"free-{i}", out var ability) || ability == null)
                continue;

            if (!finishingPicked.Add(ability.Value))
                continue;

            build.FreeBoosts.Add(ability.Value);
        }

        // Int can change free class-skill slot count — trim excess picks.
        if (build.Class != null
            && _prototypeManager.TryIndex(build.Class.Value, out CFClassPrototype? cls))
        {
            var preview = Profile.WithCFBuild(build);
            var mods = CFAttributeMath.Snapshot(_prototypeManager, preview).Modifiers;
            var need = CFSkills.ClassSkillSlotCount(cls, mods);
            if (build.ClassTrainedSkills.Count > need)
                build.ClassTrainedSkills = build.ClassTrainedSkills.Take(need).ToList();
        }

        ClosePicker();
        SetBuild(build);
    }

    private void ShowSkillTraining()
    {
        _skillMode = SkillAssignMode.Background;
        var build = CurrentBuild();
        if (build.Background == null
            || !_prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
        {
            SkillView.Show(
                Loc.GetString("cf-character-builder-skill-training"),
                Loc.GetString("cf-character-builder-skill-need-background"),
                Array.Empty<SkillSlotDefinition>());
            OpenSkillView();
            return;
        }

        var defaults = CFSkills.BackgroundSkillSlots(background);
        if (defaults.Count == 0)
        {
            SkillView.Show(
                Loc.GetString("cf-character-builder-skill-training"),
                Loc.GetString("cf-character-builder-skill-need-background"),
                Array.Empty<SkillSlotDefinition>());
            OpenSkillView();
            return;
        }

        var labels = SkillLabelMap();
        var allowed = labels.Keys.OrderBy(id => labels[id], StringComparer.CurrentCultureIgnoreCase).ToList();
        var slots = new List<SkillSlotDefinition>();
        for (var i = 0; i < defaults.Count; i++)
        {
            string? selected = null;
            if (i < build.BackgroundTrainedSkills.Count)
                selected = build.BackgroundTrainedSkills[i].Id;
            else
                selected = defaults[i].Id;

            slots.Add(new SkillSlotDefinition
            {
                Id = $"bg-skill-{i}",
                Label = Loc.GetString("cf-character-builder-skill-slot", ("index", i + 1)),
                Allowed = allowed,
                Labels = labels,
                Selected = selected,
            });
        }

        SkillView.Show(
            Loc.GetString("cf-character-builder-skill-training"),
            Loc.GetString("cf-character-builder-skill-help"),
            slots);
        OpenSkillView();
    }

    private void ShowClassSkill()
    {
        _skillMode = SkillAssignMode.Class;
        var build = CurrentBuild();
        if (Profile == null
            || build.Class == null
            || !_prototypeManager.TryIndex(build.Class.Value, out CFClassPrototype? cls))
        {
            SkillView.Show(
                Loc.GetString("cf-character-builder-class-skill"),
                Loc.GetString("cf-character-builder-class-skill-need-class"),
                Array.Empty<SkillSlotDefinition>());
            OpenSkillView();
            return;
        }

        var sheet = CFAttributeMath.Snapshot(_prototypeManager, Profile);
        var slotCount = CFSkills.ClassSkillSlotCount(cls, sheet.Modifiers);
        if (slotCount == 0)
        {
            SkillView.Show(
                Loc.GetString("cf-character-builder-class-skill"),
                Loc.GetString("cf-character-builder-class-skill-need-class"),
                Array.Empty<SkillSlotDefinition>());
            OpenSkillView();
            return;
        }

        var blocked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var skill in cls.Proficiencies.Skills.Trained)
            blocked.Add(skill);

        if (build.BackgroundTrainedSkills.Count > 0)
        {
            foreach (var skill in build.BackgroundTrainedSkills)
                blocked.Add(skill.Id);
        }
        else if (build.Background != null
                 && _prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
        {
            foreach (var skill in CFSkills.BackgroundSkillSlots(background))
                blocked.Add(skill.Id);
        }

        var labels = SkillLabelMap();
        var allowed = labels.Keys
            .Where(id => !blocked.Contains(id))
            .OrderBy(id => labels[id], StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var slots = new List<SkillSlotDefinition>();
        for (var i = 0; i < slotCount; i++)
        {
            string? selected = i < build.ClassTrainedSkills.Count
                ? build.ClassTrainedSkills[i].Id
                : null;
            if (selected != null && !allowed.Contains(selected))
                selected = null;

            slots.Add(new SkillSlotDefinition
            {
                Id = $"class-skill-{i}",
                Label = Loc.GetString("cf-character-builder-class-skill-slot", ("index", i + 1)),
                Allowed = allowed,
                Labels = labels,
                Selected = selected,
            });
        }

        SkillView.Show(
            Loc.GetString("cf-character-builder-class-skill"),
            Loc.GetString("cf-character-builder-class-skill-help"),
            slots);
        OpenSkillView();
    }

    private void AcceptSkillAssignment()
    {
        if (Profile == null)
            return;

        var selections = SkillView.GetSelections();
        var build = CurrentBuild();

        if (_skillMode == SkillAssignMode.Background)
        {
            if (build.Background == null
                || !_prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
            {
                ClosePicker();
                return;
            }

            var need = CFSkills.BackgroundSkillSlots(background).Count;
            build.BackgroundTrainedSkills.Clear();
            var picked = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < need; i++)
            {
                if (!selections.TryGetValue($"bg-skill-{i}", out var skillId) || skillId == null)
                    continue;
                if (!picked.Add(skillId))
                    continue;
                build.BackgroundTrainedSkills.Add(skillId);
            }
        }
        else
        {
            if (build.Class == null
                || !_prototypeManager.TryIndex(build.Class.Value, out CFClassPrototype? cls))
            {
                ClosePicker();
                return;
            }

            var sheet = CFAttributeMath.Snapshot(_prototypeManager, Profile);
            var need = CFSkills.ClassSkillSlotCount(cls, sheet.Modifiers);
            build.ClassTrainedSkills.Clear();
            var picked = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < need; i++)
            {
                if (!selections.TryGetValue($"class-skill-{i}", out var skillId) || skillId == null)
                    continue;
                if (!picked.Add(skillId))
                    continue;
                build.ClassTrainedSkills.Add(skillId);
            }
        }

        ClosePicker();
        SetBuild(build);
    }

    private Dictionary<string, string> SkillLabelMap()
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var skill in _prototypeManager.EnumeratePrototypes<CFAbilitySkillPrototype>())
            labels[skill.ID] = Loc.GetString(skill.Name);
        return labels;
    }

    private void UpdateBuilderSummary()
    {
        var ancestry = Loc.GetString("cf-character-builder-none");
        if (Profile != null && _prototypeManager.TryIndex(Profile.Species, out SpeciesPrototype? species))
            ancestry = Loc.GetString(species.Name);

        var build = CurrentBuild();

        var background = Loc.GetString("cf-character-builder-none");
        if (build.Background != null && _prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? backgroundProto))
            background = Loc.GetString(backgroundProto.Name);

        var clsName = Loc.GetString("cf-character-builder-none");
        if (build.Class != null && _prototypeManager.TryIndex(build.Class.Value, out CFClassPrototype? classProto))
            clsName = Loc.GetString(classProto.Name);

        var jobName = Loc.GetString("cf-character-builder-none");
        if (Profile != null)
        {
            var highPriorityJob = Profile.JobPriorities.FirstOrDefault(priority => priority.Value == JobPriority.High).Key;
            if (!string.IsNullOrEmpty(highPriorityJob) && _prototypeManager.TryIndex<JobPrototype>(highPriorityJob, out var job))
                jobName = job.LocalizedName;
        }

        AncestryButton.Text = $"{Loc.GetString("cf-character-builder-ancestry")}: {ancestry}";
        BackgroundButton.Text = $"{Loc.GetString("cf-character-builder-background")}: {background}";
        ClassButton.Text = $"{Loc.GetString("cf-character-builder-class")}: {clsName}";
        JobButton.Text = $"{Loc.GetString("cf-character-builder-job")}: {jobName}";

        var heritageName = Loc.GetString("cf-character-builder-none");
        if (build.Heritage != null && _prototypeManager.TryIndex(build.Heritage.Value, out CFHeritagePrototype? chosenHeritage))
            heritageName = Loc.GetString(chosenHeritage.Name);
        HeritageSlot.Text = Loc.GetString("cf-character-builder-heritage-selected", ("name", heritageName));

        var featName = Loc.GetString("cf-character-builder-none");
        if (build.AncestryFeat != null && _prototypeManager.TryIndex(build.AncestryFeat.Value, out CFFeatPrototype? chosenFeat))
            featName = Loc.GetString(chosenFeat.Name);
        AncestryFeatSlot.Text = Loc.GetString("cf-character-builder-ancestry-feat-selected", ("name", featName));

        var classFeatName = Loc.GetString("cf-character-builder-none");
        if (build.ClassFeat != null && _prototypeManager.TryIndex(build.ClassFeat.Value, out CFFeatPrototype? chosenClassFeat))
            classFeatName = Loc.GetString(chosenClassFeat.Name);
        ClassFeatSlot.Text = Loc.GetString("cf-character-builder-class-feat-selected", ("name", classFeatName));

        var classFeatLevel2Name = Loc.GetString("cf-character-builder-none");
        if (build.ClassFeatLevel2 != null && _prototypeManager.TryIndex(build.ClassFeatLevel2.Value, out CFFeatPrototype? chosenClassFeatLevel2))
            classFeatLevel2Name = Loc.GetString(chosenClassFeatLevel2.Name);
        Level2FeatSlot.Text = Loc.GetString("cf-character-builder-class-feat-level-2-selected", ("name", classFeatLevel2Name));

        var spriteName = Loc.GetString("cf-sprite-humanoid");
        if (Profile != null)
        {
            var spriteAncestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
            var spriteMatch = spriteAncestry?.Sprites.FirstOrDefault(sprite => sprite.Species == Profile.Species);
            if (spriteMatch != null)
                spriteName = Loc.GetString(spriteMatch.Name);
            else if (Profile.Species.Id is "Human" or "Dwarf")
                spriteName = ancestry;
        }
        SpriteSlot.Text = Loc.GetString("cf-character-builder-sprite-selected", ("name", spriteName));

        var boostComplete = IsBoostAssignmentComplete(build);
        SetAbilitiesButton.Text = boostComplete
            ? Loc.GetString("cf-character-builder-set-abilities-done")
            : Loc.GetString("cf-character-builder-set-abilities");

        SkillTrainingButton.Text = IsSkillTrainingComplete(build)
            ? Loc.GetString("cf-character-builder-skill-training-done")
            : Loc.GetString("cf-character-builder-skill-training");

        ClassSkillButton.Text = IsClassSkillComplete(build)
            ? Loc.GetString("cf-character-builder-class-skill-done")
            : Loc.GetString("cf-character-builder-class-skill");

        UpdateHeaderStats();
    }

    private bool IsSkillTrainingComplete(CFCharacterBuildData build)
    {
        if (build.Background == null
            || !_prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background))
            return false;

        var need = CFSkills.BackgroundSkillSlots(background).Count;
        return need > 0 && build.BackgroundTrainedSkills.Count >= need;
    }

    private bool IsClassSkillComplete(CFCharacterBuildData build)
    {
        if (Profile == null
            || build.Class == null
            || !_prototypeManager.TryIndex(build.Class.Value, out CFClassPrototype? cls))
            return false;

        var sheet = CFAttributeMath.Snapshot(_prototypeManager, Profile);
        var need = CFSkills.ClassSkillSlotCount(cls, sheet.Modifiers);
        return need > 0 && build.ClassTrainedSkills.Count >= need;
    }

    private bool IsBoostAssignmentComplete(CFCharacterBuildData build)
    {
        if (Profile == null)
            return false;

        var ancestry = CFAttributeMath.FindAncestry(_prototypeManager, Profile.Species);
        if (ancestry != null)
        {
            var need = CFAttributeMath.CountAncestryFreeBoosts(ancestry);
            if (build.AncestryFreeBoosts.Count < need)
                return false;
        }

        if (build.Background != null)
        {
            if (build.BackgroundFreeBoost == null)
                return false;

            if (_prototypeManager.TryIndex(build.Background.Value, out CFBackgroundPrototype? background)
                && CFAttributeMath.BackgroundBoostChoices(background).Count > 0
                && build.BackgroundBoost == null)
            {
                return false;
            }
        }

        return build.FreeBoosts.Count >= CFAttributeMath.FinishingFreeBoostCount;
    }

    private void UpdateHeaderStats()
    {
        if (Profile == null)
            return;

        var sheet = CFAttributeMath.Snapshot(_prototypeManager, Profile);
        var mods = sheet.Modifiers;
        var ancestry = sheet.Ancestry;

        CFHeritagePrototype? heritage = null;
        if (Profile.CFBuild.Heritage != null)
            _prototypeManager.TryIndex(Profile.CFBuild.Heritage.Value, out heritage);
        if (heritage != null && !heritage.Fits(ancestry?.ID))
            heritage = null;

        var size = ancestry == null && heritage?.Size == null
            ? Loc.GetString("cf-character-builder-none")
            : Loc.GetString($"cf-size-{CFAttributeMath.EffectiveSize(ancestry, heritage).ToString().ToLower()}");
        var speedFeet = CFAttributeMath.EffectiveSpeed(ancestry, heritage);
        var speed = ancestry == null && heritage == null
            ? Loc.GetString("cf-character-builder-none")
            : $"{speedFeet} {Loc.GetString("cf-character-builder-feet")}";

        SizeLabel.Text = Loc.GetString("cf-character-builder-size", ("size", size));
        SpeedLabel.Text = Loc.GetString("cf-character-builder-speed", ("speed", speed));
        StrLabel.Text = Loc.GetString("cf-character-builder-str", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Strength])));
        DexLabel.Text = Loc.GetString("cf-character-builder-dex", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Dexterity])));
        ConLabel.Text = Loc.GetString("cf-character-builder-con", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Constitution])));
        IntLabel.Text = Loc.GetString("cf-character-builder-int", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Intelligence])));
        WisLabel.Text = Loc.GetString("cf-character-builder-wis", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Wisdom])));
        ChaLabel.Text = Loc.GetString("cf-character-builder-cha", ("score", CFAbilityScores.FormatModifier(mods[CFAbility.Charisma])));

        HpSummary.Text = Loc.GetString("cf-character-builder-hp-summary", ("current", sheet.MaxHp), ("max", sheet.MaxHp));
        AcValue.Text = sheet.ArmorClass.ToString();

        FortitudeLabel.Text = SaveLine("cf-character-builder-save-fortitude",
            CFAttributeMath.IsSaveTrained(sheet.Class, "Fortitude"),
            sheet.Fortitude);
        ReflexLabel.Text = SaveLine("cf-character-builder-save-reflex",
            CFAttributeMath.IsSaveTrained(sheet.Class, "Reflex"),
            sheet.Reflex);
        WillLabel.Text = SaveLine("cf-character-builder-save-will",
            CFAttributeMath.IsSaveTrained(sheet.Class, "Will"),
            sheet.Will);

        SpellsTab.Rebuild(Profile, _prototypeManager, SetBuild);
    }

    private static string SaveLine(string saveLoc, bool trained, int bonus)
    {
        var rank = Loc.GetString(trained
            ? "cf-character-builder-prof-trained-short"
            : "cf-character-builder-prof-untrained-short");
        return Loc.GetString("cf-character-builder-save-line",
            ("rank", rank),
            ("bonus", CFAbilityScores.FormatModifier(bonus)),
            ("save", Loc.GetString(saveLoc)));
    }

    public void RefreshEquipmentTabs()
    {
        var collection = IoCManager.Instance;
        var session = _playerManager.LocalSession;
        HumanoidCharacterProfile? GetProfile() => Profile;

        void OnChanged(HumanoidCharacterProfile updated)
        {
            Profile = updated;
            SetDirty();
            ReloadPreview();
            WeaponsTab.Apply(updated);
            DefenseTab.Apply(updated);
            GearTab.Apply(updated);
            ShopTab.Apply(updated);
        }

        WeaponsTab.Rebuild(Profile, JobOverride, session, collection, _prototypeManager, GetProfile, OnChanged);
        DefenseTab.Rebuild(Profile, JobOverride, session, collection, _prototypeManager, GetProfile, OnChanged);
        GearTab.Rebuild(Profile, JobOverride, session, collection, _prototypeManager, GetProfile, OnChanged);
        ShopTab.Rebuild(Profile, JobOverride, session, collection, _prototypeManager, GetProfile, OnChanged);
        SpellsTab.Rebuild(Profile, _prototypeManager, SetBuild);
    }

    public void FocusGearTab(JobPrototype job)
    {
        ClosePicker();
        JobOverride = job;
        RefreshEquipmentTabs();
        TabContainer.CurrentTab = GearTabIndex;
        ReloadPreview();
    }

    private static string Stat(string locId, string value)
    {
        return Loc.GetString(locId, ("value", value));
    }

    private static string JoinLoc(IEnumerable<LocId> ids)
    {
        var names = ids.Select(id => Loc.GetString(id)).ToArray();
        return names.Length == 0 ? Loc.GetString("cf-character-builder-none") : string.Join(", ", names);
    }

    private static string FormatAbilities(IEnumerable<CFAbility> abilities)
    {
        var names = abilities.Select(AbilityName).ToArray();
        return names.Length == 0 ? Loc.GetString("cf-character-builder-none") : string.Join(", ", names);
    }

    private static string AbilityName(CFAbility ability)
    {
        return Loc.GetString($"cf-ability-{ability.ToString().ToLower()}");
    }

    private static string FormatBoosts(IEnumerable<CFAbilityBoost> boosts)
    {
        var names = boosts.Select(boost => boost == CFAbilityBoost.Free
            ? Loc.GetString("cf-character-builder-free")
            : AbilityName((CFAbility)boost)).ToArray();
        return names.Length == 0 ? Loc.GetString("cf-character-builder-none") : string.Join(", ", names);
    }

    private string FormatSkills(IEnumerable<ProtoId<CFAbilitySkillPrototype>> boosts, IEnumerable<ProtoId<CFAbilitySkillPrototype>> trained)
    {
        var names = boosts.Concat(trained).Distinct().Select(id =>
        {
            if (_prototypeManager.TryIndex(id, out CFAbilitySkillPrototype? skill))
                return Loc.GetString(skill.Name);
            return id.ToString();
        }).ToArray();
        return names.Length == 0 ? Loc.GetString("cf-character-builder-none") : string.Join(", ", names);
    }

    private static string DescribeProficiencies(CFClassProficiencies proficiencies)
    {
        var lines = new List<string>();
        AppendProficiency(lines, Loc.GetString("cf-character-builder-prof-perception"), proficiencies.Perception);
        AppendProficiency(lines, Loc.GetString("cf-character-builder-prof-saves"), proficiencies.SavingThrows);
        AppendProficiency(lines, Loc.GetString("cf-character-builder-prof-skills"), proficiencies.Skills);
        AppendProficiency(lines, Loc.GetString("cf-character-builder-prof-attacks"), proficiencies.Attacks);
        AppendProficiency(lines, Loc.GetString("cf-character-builder-prof-defenses"), proficiencies.Defenses);
        return string.Join('\n', lines);
    }

    private static void AppendProficiency(List<string> lines, string label, CFProficiencyList list)
    {
        AddRank(lines, "cf-character-builder-trained", label, list.Trained);
        AddRank(lines, "cf-character-builder-expert", label, list.Expert);
        AddRank(lines, "cf-character-builder-master", label, list.Master);
        AddRank(lines, "cf-character-builder-legendary", label, list.Legendary);
    }

    private static void AddRank(List<string> lines, string loc, string label, List<string> items)
    {
        if (items.Count == 0)
            return;

        lines.Add(Loc.GetString(loc, ("items", $"{label}: {string.Join(", ", items)}")));
    }
}
