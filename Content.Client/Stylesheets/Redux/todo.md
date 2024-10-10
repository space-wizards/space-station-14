- [x] Inventory `[X]` button looks weird
    - Styles for `TextureButton` were not implemented
- [x] Right click menu has no colors
    - Ported `ContextMenu` styles from `StyleNano.cs`
- [x] Colors are slightly too dark IMO
- [x] Text when editing paper is white
- [x] Action buttons don't have highlighting
- [x] `ButtonSmall`
- [x] `[X]` button is misaligned on FancyWindow also title text too kinda
- [x] Move `ISheetletConfig` classes into their own directory
- [x] Create a HUD stylesheet for examine, right click, chat etc. perhaps, distinguish from NT Ui
    - [x] ~~seperated chat ui~~ (looks funky)
    - [ ] ~~Admin / debug menus~~ (do in another PR)
    - [x] ~~Tooltips!~~ (weird)
- [x] ~~Stamps look weird?~~ they look fine
- [x] `ScrollContainer` has no scrollbar!!!
- [x] `NavMapControl.cs:133` fix this
- [x] `CrewMonitoringWindow` uses `TooltipDesc` for some reason??
- [x] Whatever the hell `StyleClassSliderWhite` and friends are being used for
    - nothing, apparently
- [x] Move `Chat` style classes from `StyleClass.cs`
- [ ] ~~Vending machines entries no hover?~~
    - `ItemList` skill issue
- [x] Also `ApcMenu.xaml.cs`: localize watts
- [ ] ~~`PopupUiController` lots of hardcoding~~
    - Ehhh probably fine
- [ ] ~~`ScopedResCache` because moving around resources is annoying and prone to error when merging~~
    - You can have multiple roots
    - [x] `GetResourceOr` and also redo common sheetlets to use this instead
    - [x] and since you'll have to refactor that anyway, move all the resource paths into `ISheetletConfig`s
- [x] `MainMenuSheetlet` should maybe be with the xaml?
- [x] `Palette` class and kick out indexing palette with a number
    - `ColorPalette` / `Palettes` but same thing
- [x] tooltips being part of another stylesheet is kinda bad
- [x] rename `InterfaceStylesheet` to `SystemStylesheet`
- [ ] ~~rename `FancyWindow` to `NanoWindow`~~
    - doesnt work with other stylesheets so `FancyWindow` stays!
    - [x] add a `Stylesheet` property, so you can like, set the styles of a window
- [x] Get rid of `NTSheetlets` & `InterfaceSheetlets`
    - Eh, most of it anyway
- [x] Make all `StyleClass` conform to naming conventions
- [x] Separate out `FontKind` maybe
- [x] change `ButtonHovered` and friends to `PseudoHovered`
- [x] `IPanelPalette` is unnecessary now
- [x] What the fuck is `StyleSpace` get rid of it
    - Switched to `SystemStylesheet`
- [x] Fix the codepen
- [ ] figure out `BaseStylesheet.Fonts`

#### HARDCODED SHIT (non exhaustive list)

- COLORS
    - [ ] `StampWidget.xaml`
    - [ ] `AirAlarmWindow.xaml.cs`
    - [ ] `ReplayMainMenuControl.xaml`
    - [ ] `LobbyGui.xaml`
    - [ ] `MenuButton.cs`
- [ ] `WindowSheetlet` `NanoHeading` hardcoded classnames
- [ ] `LabelSheetlet` / `TextSheetlet` dont hardcode the label sizes

TL;DR port all of `StyleNano` into sheetlets

### Another PR:

- [ ] Style classes for window sizes (and yayyy refactor all the windows maybe?)
- [ ] Admin Message window exit button is basically indistinguishable from the red header
- [ ] Button text colors / change text color when pressed / disabled?
- [ ] Resizing windows is like, too precise
- [ ] kill `DefaultWindow`
- [ ] Top menu button spacing inconsistent
- [ ] Guidebook opens to the right? (probably fine)
- [ ] Create a syndicate (antagonist?) stylesheet for uplink, syndicate consoles
- [ ] Make a consistent `Tooltip` component that can be used for `ActionAlertTooltip`, examine tooltip, and w/e
- [ ] Character info screen popup button thing stays pressed when you dont use `C` to close the popup window
- [ ] `OptionButton` looks kinda gross :(
- [ ] Make all admin menus a different palette
- [ ] Shadows on windows?
- [ ] `HLine` and probably other classes in RobustToolbox dont have `StyleClass<X>` props
- [ ] `ApcMenu.xaml.cs` maybe make the text color fully dynamic
- [ ] Have `ScrollContainer`s remember how much you've scrolled?

### Design Decisions

As anyone who's had the misfortune of editing `StyleNano` is probably painfully aware of, the main problem
with `StyleNano` is how difficult it is to find anything. Maybe the style classes you want are sitting somewhere in that
gargantuan tangle of code, or maybe not. Maybe it;s just easier to hardcode everything in the UI code instead (WHICH IS
THE ROUTE LOT OF PEOPLE HAVE (rightfully) TAKEN (stylenano was a complete fucking mess)). With this new iteration, I
tried to maximize the readability of the *structure* of the code. Want to know what style classes are available for you
to use? Look in `StyleClass`. Want to know the styles applicable to labels? `LabelSheetlet` or `TextSheetlet`.
Buttons? `ButtonSheetlet`.

But what are `Sheetlet`s? Good question person-who-didnt-read-the-original-PR! Basically how styles used to work is
every single style rule would be agglomerated in one unholy massive fucking list. This is still how it works, but now
the responsibility of chipping in styles to this massive list is spread out among all the sheetlets. They have one
method, `StyleRule[] GetRules`, and all these rules from all the sheetlets are collected up to do in like 30-40ish files
what used to be one 1600 line file.

`ISheetletConfig` is intended to cut down on repeated code by providing shared functionality between the stylesheets.
Its literally only used for buttons. Its also used (in my crusade against anything hardcoded) to store resource paths,
because it's easier to reference if it's all centralized.

#### Deviations from Original PR's Direction

The class holding all the style classes, [`StyleClass`] was originally named `Styleclasses`. I think this was a better
name. Unfortunately, `Control`s have a field with the exact same name, meaning to reference `Styleclasses`, you would
have to type out `Stylesheets.Redux.Styleclasses.<whatever>`. This syntax would be shortened after everything is moved
out of `Redux` but still, annoying. The shorter syntax is nicer. Also as an extra bonus, it's closer to the syntax of
something like `StyleClassLabelHeading`. Just add a dot! That's kinda neat.

<sub>class stopped sounding like a real word. Class class class class class. Blegh.</sub>

Instead of an array of colors, I made a custom [`ColorPalette`] class. This is because when transferring all the styles
into `Sheetlet`s, I noticed I basically used the palette in one of three ways, foreground elements (Buttons etc.),
background elements, (Panels, etc.), and text. So I represented that in the palette! Just makes code a bit more
readable / robust. Also, I just kinda hated that `[0]` was the brightest and `[4]` the darkest. It should be the other
way around! and what if I want to add more colors? There are more than five shades of any given color!

This does not compromise customizability because any colors can be curly-brace initialized after the fact if you are
really motivated.

What moony originally did in their PR was they had resources scoped / access-locked (which I have kept), and, as a
consequence, sheetlets that used a resource would have to be sheet-specific. This included buttons, panels, windows,
etc.; things that basically every stylesheet would want, and which would have to be duplicated for every stylesheet. I
think the intention behind this is to prevent resources intended for one stylesheet being used unintentionally in
another, but honestly, the organization of the resources folder is a mess (a lot of style-generic resources are sitting
in `Textures/Interface/Nano`), and this would lead to a lot of copying resources, which is definitely a maintenance
hazard. I could clean it up, but then it could be a NIGHTMARE to merge for downstream forks (probably idk) (also
touching the resource folder scares me). The resource-scoping system does allow you to specify multiple scopes to try in
order, but that's kinda icky. So! I propose (and have already implemented) the following solution:

##### `GetResourceOr`

<sub>(and `GetTextureOr`)</sub>

It gets the scoped resource like normal, but if it doesn't exist, falls back to an absolute root. Is it a generic
resource that has no business being is `Textures/Interface/Nano`? I don't care! It can stay there! I really can't be
bothered to move it! Now the sheetlet will work with any stylesheet. If the scope happens to exist, it'll use that
instead.

This method is kinda clunky but that's good (maybe?) because you want to use it where it counts. If there's a resource
that, ideally, every theme should have a unique implementation of, then you would just use `GetResource`.

### Significant Interface Changes

- Colors are different (duh)
- Tooltips on action menu now is slightly transparent for consistency w/ examine popup
- Everything else should pretty much be the same. I'm like 80% sure I transferred everything from `StyleNano` over
  correctly, as I was pretty methodical with it, but its also entirely possible I forgot something. If anybody notices a
  UI that looks worse than they remember, double check it actually is different (this happened to me several times, some
  of the UIs are just kinda bad), and pretty please PR the changes (or ask me, and I'll probably do it).
- I definitely like messed a couple colors up but whatever

### Breaking Changes

#### `StyleNano` is gone!!

- Well it's not gone, but it is obsolete. If you have messed with it at all, your changes will need to be redone.
- Style classes defined in `StyleNano` have been moved either to `Content.Client.Stylesheets.Redux.StyleClass` or their
  associated `Control`.
- Any `StyleRule` additions to `StyleNano` must be rewritten to conform to the new format (See guide (TODO: LINK GUIDE))
- Some unused / redundant style classes were removed. If you hapenned to be relying on them, either substitute in the
  appropriate style classes defined in `Content.Client.Stylesheets.Redux.StyleClass` or create a new Sheetlet (Again,
  see guide)
- If you are planning on creating a new UI, there are new conventions you should follow (Yet again, see guide)
- `StyleSpace` is gone too but that probably doesn't affect you

#### `Content.Client/Stylesheets/Redux`

Since this was such a large refactor, all the code is in `Content.Client/Stylesheets/Redux`. For now all the new code is
staying in `Redux` so downstream forks will have an easier time merging this change.

#### Also

Some resources have been moved around.


