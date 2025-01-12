# Harmony Contributing Guidelines

Generally, we follow [upstream's PR guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) for code quality and such.

Importantly, do not make webedits. Copied verbatim from above:
> Do not use GitHub's web editor to create PRs. PRs submitted through the web editor may be closed without review.

When you submit a PR, it is implied that you have thoroughly tested your changes, so you are expected to have a [development environment](https://docs.spacestation14.com/en/general-development/setup/setting-up-a-development-environment.html) set up.

Upstream is the [space-wizards/space-station-14](https://github.com/space-wizards/space-station-14) repository that wizden runs on.

# Table of contents

<!--ts-->
- [Content specific to Harmony](#content-specific-to-harmony)
- [Changes to upstream files](#changes-to-upstream-files)
  - [Examples of comments in upstream files](#examples-of-comments-in-upstream-files)
- [Porting content from other forks](#porting-content-from-other-forks)
- [Mapping](#mapping)
- [Art and Spriting](#art-and-spriting)
- [Before you submit](#before-you-submit)
- [Changelogs](#changelogs)
- [Additional resources](#additional-resources)
<!--te-->

# Content specific to Harmony

In general, anything you create from scratch or can put in a separate file without modifying the upstream code should go in a Harmony subfolder, `_Harmony`.

Examples:
- `Content.Server/_Harmony/Speech/EntitySystems/IlleismAccentSystem.cs`
- `Resources/Prototypes/_Harmony/game_presets.yml`
- `Resources/Audio/_Harmony/Lobby/harmonilla.ogg`
- `Resources/Textures/_Harmony/Clothing/Shoes/Misc/ducky-galoshes.rsi`
- `Resources/Locale/en-US/_Harmony/game-ticking/game-presets/preset-deathmatchpromod.ftl`

# Changes to upstream files

If you make a change to an upstream C# or YAML file, **you must add comments on or around the changed lines**. The comments should clarify what changed, to make conflict resolution simpler when a file is changed upstream.

If you delete any upstream code, comment it out instead of removing it.

For YAML specifically, if you add a new component to a prototype, add the comment to the `type: ...` line.
If you only modify some fields of a component, comment the fields instead.

For C# files, if you are adding a lot of code, try to put it in a partial class when it makes sense.

The exception to this is the early merge of upstream cherry-picked commits that are going to be merged in the Upstream Merge by maintainers regardless. There's no harm in leaving them as-is.

As an aside, fluent (.ftl) files **do not support comments on the same line** as a locale value, so be careful when changing them.

## Examples of comments in upstream files

A single line comment on a changed yml field:
```yml
- type: loadoutGroup
  id: GroupSpeciesBreathTool
  name: loadout-group-breath-tool
  minLimit: 1
  maxLimit: 1
  hidden: false # Harmony change for various Vox masks
...
```

A pair of comments enclosing a list of added items to starting gear:
```yml
  loadouts:
...
  - ClothingNeckAutismPin
  - ClothingNeckGoldAutismPin
# Begin Harmony specific trinkets
  - ClothingNeckVulturePin
  - ClothingNeckMirosPin
  - ClothingNeckLizardPin
...
# End Harmony specific trinkets
```

Commented-out removed upstream code:
```yml
- type: loadoutGroup
...
  loadouts:
...
  - JumpsuitHoSTurtleNeckHarmony # Harmony
  - JumpskirtHoSTurtleNeckHarmony # Harmony
# - HeadofSecurityTurtleneck
# - HeadofSecurityTurtleneckSkirt
...
```

A single line comment on one changed line:
```cs
...
if (!_actionBlocker.CanSpeak(source, true) && !ignoreActionBlocker) // Harmony change for hypophonia trait
...
```

A pair of comments enclosing a block of added code:
```cs
public void SetCriminalIcon(string name, SecurityStatus status, EntityUid characterUid)
{
...
    record.StatusIcon = status switch
    {
...
        SecurityStatus.Suspected => "SecurityIconSuspected",
        // Harmony - start of additional statuses
        SecurityStatus.Monitor => "SecurityIconMonitor",
        SecurityStatus.Search => "SecurityIconSearch",
        // Harmony - end of additional statuses
        _ => record.StatusIcon
    };
...
}
```

# Porting content from other forks

In general, the same rules as for Harmony-specific content apply, but use the namespace of the fork instead.

Example:
- `Resources/Prototypes/_Umbra/game_presets.yml`
- `Content.Server/_DV/Objectives/Components/TeachLessonConditionComponent.cs`

If the namespace in the source fork doesn't start with `_` and there is no content in Harmony that was ported from this fork before, use your best judgment in picking the namespace folder name that will unmistakably indicate the fork the code is taken from.

If you are not sure which namespace to use, ask for help in the `#harmony-contributors` channel on our Discord.

# Mapping

Game maps are held to [upstream mapping guidelines](https://docs.spacestation14.com/en/space-station-14/mapping.html).

Maps/grids should be created in develompent environment or a [local Harmony build](http://cdn.harmony14.com/fork/harmony/) and should always be fully tested to ensure power, atmospherics, gravity, etc are functional before submitting.

Harmony exclusive maps and related grids (i.e. shuttles/salvage wrecks) should visually fit the environment of SS14 and allow for immersion within an MRP server. This is a largely subjective guideline and is subject to maintainer/project manager discretion.

Upstream maps should remain unchanged whenever possible. Significant changes introduce difficult merge conflicts and may require maintaining seperatley from upstream, which adds undesired maptainer workload and may lead to derotation.
Except for necessary Harmony exclusive content, upstream map changes should be PR'd to upstream.

# Art and Spriting

Test any and all sprites in-game within your develompent environment and provide screenshots in the Media section of your PR. The screenshots should be taken within the game, not in the editor, and must showcase all the sprites.

Currently we have no definitive pallete, that is intentional, if your art is a better sprite but changes the pallete a bit, that's entirely fine.

Change isn't bad, it's encouraged.

# Before you submit

Double-check your diff on GitHub before submitting: look for unintended commits or changes and remove accidental whitespace, line-ending, and indentation changes.

Double-check that your PR is targeting the correct repository and branch. The target repository should be `ss14-harmony/ss14-harmony` and the branch should be `master`. If you make a mistake in the target selection, you will PR all of Harmony changes to upstram codebase.

Additionally, for long-lasting PRs, if you see `RobustToolbox` in the changed files, you have to revert it. Use:
```sh
git checkout upstream/master RobustToolbox
```
(Replace `upstream` with the name of your `ss14-harmony/ss14-harmony` remote.)

# Changelogs

By default, any changelogs in a line after `:cl:` go in the Harmony changelog.

Harmony currently doesn't maintain an Admin changelog.

# Additional resources

If you are new to contributing to SS14 in general, have a look at the [SS14 docs](https://docs.spacestation14.io/) or ask for help in the `#harmony-contributors` channel on our Discord.
