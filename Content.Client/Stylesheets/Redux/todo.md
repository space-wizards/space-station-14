- [x] Inventory `[X]` button looks weird
    - Styles for `TextureButton` were not implemented
- [x] Right click menu has no colors
    - Ported `ContextMenu` styles from `StyleNano.cs`
- [ ] Admin Message window exit button is basically indistinguishable from the red header
- [x] Colors are slightly too dark IMO
- [x] Text when editing paper is white
- [ ] Stamps look weird?
- [ ] `ScrollContainer` has no scrollbar!!!
- [ ] Action buttons don't have highlighting
- [ ] lathes open to the right (also make them fancy)
- [ ] Resizing windows is like, too precise
- [ ] `NavMapControl.cs:133` fix this
- [x] Create a HUD stylesheet for examine, right click, chat etc. perhaps, distinguish from NT Ui
    - [ ] (remove `ContextMenuSheetlet.cs:16`)
    - [x] seperated chat ui
    - [ ] Admin / debug menus
    - [ ] I cheat on the "resources are access locked" thing in `ContextMenuSheetlet.cs:36`. This needs to be fixed!
    - [x] Tooltips!
- [ ] `BaseSheetlets`
- [ ] `CrewMonitoringWindow` uses `TooltipDesc` for some reason??
- [ ] Enum for accessing palette?
- [ ] Button text colors / change text color when pressed / disabled?
- [ ] `MenuButton.cs` hardcoded colors
- [x] `ButtonSmall`
- [ ] Whatever the hell `StyleClassSliderWhite` and friends are being used for
- [ ] Move `Chat` style classes from `StyleClass.cs`

TL;DR port all of `StyleNano` into sheetlets

### Significant Interface Changes

- Tooltips on action menu now is slightly transparent for consistency w/ examine popup

### Another PR:

- [ ] kill not-fancy windows
- [ ] Top menu button spacing inconsistent
- [ ] Guidebook opens to the right? (probably fine)
- [ ] Create a syndicate stylesheet for uplink, syndicate consoles
- [ ] `[X]` button is misaligned on FancyWindow also title text too kinda
- [ ] Make a consistent `Tooltip` component that can be used for `ActionAlertTooltip`, ``
- [ ] Character info screen popup button thing stays pressed when you dont use `C` to close the popup window
