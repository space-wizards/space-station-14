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
    - [ ] (remove `ContextMenuSheetlet.cs:16`)
    - [x] ~~seperated chat ui~~ (looks funky)
    - [ ] ~~Admin / debug menus~~ (do in another PR)
    - [ ] I cheat on the "resources are access locked" thing in `ContextMenuSheetlet.cs:36`. This needs to be fixed!
    - [x] ~~Tooltips!~~ (weird)
- [x] ~~Stamps look weird?~~ they look fine
- [x] `ScrollContainer` has no scrollbar!!!
- [x] `NavMapControl.cs:133` fix this
- [x] `CrewMonitoringWindow` uses `TooltipDesc` for some reason??
- [x] Whatever the hell `StyleClassSliderWhite` and friends are being used for
  - nothing, apparently
- [x] Move `Chat` style classes from `StyleClass.cs`
- [ ] Vending machines entries no hover?
- [ ] Also `ApcMenu.xaml.cs`: localize watts
- [ ] `PopupUiController` lots of hardcoding

- [ ] `ScopedResCache` because moving around resources is annoying and prone to error when merging
- [ ] `MainMenuSheetlet` should maybe be with the xaml?
- [ ] `Palette` class and kick out indexing palette with a number
- [ ] tooltips being part of another stylesheet is kinda bad
- [ ] rename `InterfaceStylesheet` to `SystemStylesheet`
- [ ] rename `FancyWindow` to `NanoWindow`
- [ ] Get rid of `NTSheetlets` & `InterfaceSheetlets`
- [ ] Make all `StyleClass` conform to naming conventions
- [ ] `LabelSheetlet` / `TextSheetlet` dont hardcode the label sizes
- [ ] Separate out `FontKind` maybe
- [ ] change `ButtonHovered` and friends to `PseudoHovered`

HARDCODED COLORS

- StampWidget.xaml
- AirAlarmWindow.xaml.cs
- ReplayMainMenuControl.xaml
- LobbyGui.xaml
- MenuButton.cs

TL;DR port all of `StyleNano` into sheetlets

### Design Decisions

As anyone who's had the misfortune of editing `StyleNano` is probably painfully aware of, the main problem
with `StyleNano` is how difficult it is to find anything. Maybe the style classes you want are sitting somewhere in that
gargantuan tangle of code, or maybe not. Maybe its just easier to hardcode everything in the UI code instead. With this
new iteration, I tried to maximize the readability of the *structure* of the code. Want to know what style classes are
available for you to use? Look in `StyleClass`. Want to know the styles applicable to labels? `LabelSheetlet`
or `TextSheetlet`. Buttons? `ButtonSheetlet`.

The style rule definition syntax is also pretty good IMO but that wasn't me so,

### Significant Interface Changes

- Colors are different (duh)
- Tooltips on action menu now is slightly transparent for consistency w/ examine popup
- Everything else should pretty much be the same. I'm like 80% sure I transferred everything from `StyleNano` over
  correctly, as I was pretty methodical with it, but its also entirely possible I forgot something. If anybody notices a
  UI that looks worse than they remember, double check it actually is different (this happened to me several times, some
  of the UIs are just kinda bad), and pretty please PR the changes (or ask me, and I'll probably do it).

### Breaking Changes

#### `StyleNano` is gone!!

- Style classes defined in `StyleNano` have been moved either to `Content.Client.Stylesheets.Redux.StyleClass` or their
  associated `Control`.
- Any `StyleRule` additions to `StyleNano` must be rewritten to conform to the new format (See guide (TODO: LINK GUIDE))
- Some unused / redundant style classes were removed. If were relying on them, either substitute in the appropriate
  style classes defined in `Content.Client.Stylesheets.Redux.StyleClass` or create a new Sheetlet (Again, see guide)
- If you are planning on creating a new UI, there are new conventions you should follow (Yet again, see guide)

### Another PR:

- [ ] Admin Message window exit button is basically indistinguishable from the red header
- [ ] Button text colors / change text color when pressed / disabled?
- [ ] Resizing windows is like, too precise
- [ ] kill not-fancy windows
- [ ] Top menu button spacing inconsistent
- [ ] Guidebook opens to the right? (probably fine)
- [ ] Create a syndicate stylesheet for uplink, syndicate consoles
- [ ] Make a consistent `Tooltip` component that can be used for `ActionAlertTooltip`, ``
- [ ] Character info screen popup button thing stays pressed when you dont use `C` to close the popup window
- [ ] `OptionButton` looks kinda gross :(
- [ ] Make all admin menus a different palette
- [ ] Shadows on windows?
- [ ] `HLine` and probably other classes in RobustToolbox dont have `StyleClass<X>` props
- [ ] `ApcMenu.xaml.cs` maybe make the text color fully dynamic
