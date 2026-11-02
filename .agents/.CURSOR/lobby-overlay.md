# Lobby / HumanoidProfileEditor CF overlay

How CarpFinder replaces the stock character editor without rewriting upstream Lobby files.

## Goal

- `Content.Client/Lobby/**` stays identical to SS14 upstream (merge-friendly).
- CF character builder UI + logic lives under `Content.Client/_CF/Lobby/UI/`.
- Runtime type remains `Content.Client.Lobby.UI.HumanoidProfileEditor` so `LobbyUIController` / `CharacterSetupGui` need no edits.

## Layout

```
Content.Client/Lobby/UI/HumanoidProfileEditor.*     # stock upstream (on disk, not compiled)
Content.Client/_CF/Lobby/UI/
  HumanoidProfileEditor.xaml(+.cs, Appearance, BasicInfo, Builder, FlavorText, Roles, Traits)
  CharacterBuilder/   # Weapons/Defense/Gear/Spells tabs, SpellSelectionPopup, Pf2eSelectionView, …
```

Stock partials that still match CF controls can stay compiled from Lobby if desired (`Markings.cs`, `Exports.cs`, `Sheetlet.cs`). Overridden partials are `Compile Remove`d in the csproj.

## Content.Client.csproj wiring

```xml
<!-- Exclude stock editor sources -->
<Compile Remove="Lobby\UI\HumanoidProfileEditor.xaml.cs" />
<Compile Remove="Lobby\UI\HumanoidProfileEditor.Appearance.cs" />
<!-- … BasicInfo, FlavorText, Roles, Traits … -->

<!-- AFTER XamlIL.targets import (it Includes **\*.xaml) -->
<EmbeddedResource Remove="Lobby\UI\HumanoidProfileEditor.xaml" />
<AdditionalFiles Remove="Lobby\UI\HumanoidProfileEditor.xaml" />
```

`AdditionalFiles Remove` is required: the name generator errors on multiple `HumanoidProfileEditor.xaml` files even if EmbeddedResource alone is removed.

## x:Class is mandatory under `_CF/`

Without `x:Class`, XamlIL uses the embedded resource path as the type name:

- Path `_CF/Lobby/UI/Foo.xaml` → looks for `Content.Client._CF.Lobby.UI.Foo`
- Real namespace is `Content.Client.CF…` or `Content.Client.Lobby.UI…` → **compile miss** → runtime `No precompiled XAML found for …`

Always set:

```xml
x:Class="Content.Client.Lobby.UI.HumanoidProfileEditor"
```

and for builder controls:

```xml
x:Class="Content.Client.CF.Lobby.UI.CharacterBuilder.WeaponsTab"
```

(etc. for Defense/Gear/Spells/Pf2eSelectionView)

## CharacterBuilder namespace

```csharp
namespace Content.Client.CF.Lobby.UI.CharacterBuilder;
```

Referenced from the editor XAML as:

```xml
xmlns:builder="clr-namespace:Content.Client.CF.Lobby.UI.CharacterBuilder"
```

## Character window overlay

Same pattern for the in-game character menu (`C`):

- Stock: `Content.Client/UserInterface/Systems/Character/CharacterWindow.*` + `CharacterUIController.cs` (Compile/EmbeddedResource/AdditionalFiles Remove)
- CF: `Content.Client/_CF/UserInterface/Systems/Character/` — Pathfinder sheet tab, Spells tab, Shop tab, stock Info tab
- `CFSpellsTab` lists the spells granted as actions, grouped by rank, with Cast. Prepared classes (`cfClass` `spellcasting.prepared: true`) get a Spell Preparation window: search, Add, or drag a spell onto a slot of the same rank. Spontaneous casters cast from Known Spells and do not see that button. Rituals and Activations are the known spells whose traits are Ritual or Focus.
- Sheet controls: `Content.Client/_CF/Character/` (`CFCharacterSheetTab`, `CFSheetSkillRow`, `CFShopView`) with `x:Class="Content.Client.CF.Character.…"`
- `CFShopView` is the shop body. The character menu embeds it as a tab. `CFShopWindow` hosts the same view when a merchant opens a popup.
- The shop lists non-abstract `CF*` entity prototypes that have `Item`. The footer shows the purse as pp / gp / sp / cp (10 of a coin rolls into the next). Accept sends `CFShopPurchaseEvent` only when the purse covers the cart. The server spends `CFPurseComponent` and puts each item in an empty hand, then the backpack (`back` storage), then on the ground around the player. Unpriced items cannot be bought.

## Verify

```bash
dotnet build Content.Client/Content.Client.csproj -t:Rebuild --no-restore 2>&1 \
  | grep -E 'XAMLIL|Unable to find|Build succeeded|Build FAILED'
```

Expect `Build succeeded` and **no** `Unable to find type 'Content.Client._CF…'` lines.
