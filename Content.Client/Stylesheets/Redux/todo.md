- [x] Inventory `[X]` button looks weird
    - Styles for `TextureButton` were not implemented
- [x] Right click menu has no colors
    - Ported `ContextMenu` styles from `StyleNano.cs`
- [ ] Admin Message window exit button is basically indistinguishable from the red header
- [x] Colors are slightly too dark IMO
- [ ] Text when editing paper is white
- [ ] Stamps look weird?
- [ ] `ScrollContainer` has no scrollbar!!!
- [ ] Action buttons don't have highlighting
- [ ] lathes open to the right (also make them fancy)
- [ ] Resizing windows is like, too precise
- [ ] `NavMapControl.cs:133` fix this

TL;DR port all of `StyleNano` into sheetlets

Another PR:

- [ ] kill not-fancy windows
- [ ] Top menu button spacing inconsistent
- [ ] Guidebook opens to the right? (probably fine)
- [ ] Create a HUD stylesheet for examine, right click, chat etc. perhaps, distinguish from NT Ui
    - (remove `ContextMenuSheetlet.cs:16`)
    - have the primary color be slightly transparent,
    - secondary neutral gray for seperated chat and lobby ui
    - I cheat on the "resources are access locked" thing in `ContextMenuSheetlet.cs:36`. This needs to be fixed!
- [ ] Create a syndicate stylesheet for uplink, syndicate consoles
- [ ] `[X]` button is misaligned on FancyWindow also title text too kinda
