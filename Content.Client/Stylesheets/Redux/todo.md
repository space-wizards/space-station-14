- [x] Inventory `[X]` button looks weird
    - Styles for `TextureButton` were not implemented
- [x] Right click menu has no colors
    - Ported `ContextMenu` styles from `StyleNano.cs`
- [x] Colors are slightly too dark IMO
- [x] Text when editing paper is white
- [ ] Stamps look weird?
- [ ] `ScrollContainer` has no scrollbar!!!
- [x] Action buttons don't have highlighting
- [ ] `NavMapControl.cs:133` fix this
- [x] Create a HUD stylesheet for examine, right click, chat etc. perhaps, distinguish from NT Ui
    - [ ] (remove `ContextMenuSheetlet.cs:16`)
    - [x] ~~seperated chat ui~~ (looks funky)
    - [ ] ~~Admin / debug menus~~ (do in another PR)
    - [ ] I cheat on the "resources are access locked" thing in `ContextMenuSheetlet.cs:36`. This needs to be fixed!
    - [x] ~~Tooltips!~~ (weird)
- [ ] `CrewMonitoringWindow` uses `TooltipDesc` for some reason??
- [ ] Enum for accessing palette?
- [ ] `MenuButton.cs` hardcoded colors
- [x] `ButtonSmall`
- [ ] Whatever the hell `StyleClassSliderWhite` and friends are being used for
- [ ] Move `Chat` style classes from `StyleClass.cs`
- [ ] Vending machines entries no hover?
- [x] `[X]` button is misaligned on FancyWindow also title text too kinda

- [ ] `ScopedResCache` because moving around resources is annoying and prone to error when merging
- [ ] `MainMenuSheetlet` should maybe be with the xaml?
- [ ] `Palette` class and kick out indexing palette with a number
- [ ] tooltips being part of another stylesheet is kinda bad
- [ ] rename `InterfaceStylesheet` to `SystemStylesheet`
- [ ] rename `FancyWindow` to `NanoWindow`
- [ ] Get rid of `NTSheetlets` & `InterfaceSheetlets`

TL;DR port all of `StyleNano` into sheetlets

### Design Decision Differences

### Significant Interface Changes

- Colors are different (duh)
- Tooltips on action menu now is slightly transparent for consistency w/ examine popup

### Breaking Changes

#### `StyleNano` is gone!!

- Style classes defined in `StyleNano` have been moved either to `Content.Client.Stylesheets.Redux.StyleClass` or
  their associated `Control`.
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
