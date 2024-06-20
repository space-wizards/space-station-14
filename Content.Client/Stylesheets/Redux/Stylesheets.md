# Usage
## New stylesheets
The current styling system expects you to have a few things presented up-front about your stylesheet to function:
- A primary font (in the form of a FontStack.)
- Five major five color palettes (Primary, Secondary, Positive, Negative, Highlight)<br/>
  As an example, the following is (or was, likely not updated) the palette used by NanotrasenStylesheet:<br/>
  <small>this may not render on github.</small>

|                                      Primary                                       |                                      Secondary                                       |                                      Positive                                       |                                      Negative                                       |                                      Highlight                                       |
|:----------------------------------------------------------------------------------:|:------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------:|:------------------------------------------------------------------------------------:|
| <span style="padding: 3px; background: #575B7FFF; display: flex;">primary-1</span> | <span style="padding: 3px; background: #5B5D6CFF; display: flex;">secondary-1</span> | <span style="padding: 3px; background: #3E6C45FF; display: flex;">positive-1</span> | <span style="padding: 3px; background: #CF2F2FFF; display: flex;">negative-1</span> | <span style="padding: 3px; background: #A88B5EFF; display: flex;">highlight-1</span> |
| <span style="padding: 3px; background: #3D415EFF; display: flex;">primary-2</span> | <span style="padding: 3px; background: #41424EFF; display: flex;">secondary-2</span> | <span style="padding: 3px; background: #294E2FFF; display: flex;">positive-2</span> | <span style="padding: 3px; background: #9E1D1EFF; display: flex;">negative-2</span> | <span style="padding: 3px; background: #806843FF; display: flex;">highlight-2</span> |
| <span style="padding: 3px; background: #2A2C43FF; display: flex;">primary-3</span> | <span style="padding: 3px; background: #2C2D37FF; display: flex;">secondary-3</span> | <span style="padding: 3px; background: #1A371FFF; display: flex;">positive-3</span> | <span style="padding: 3px; background: #751012FF; display: flex;">negative-3</span> | <span style="padding: 3px; background: #5F4B2EFF; display: flex;">highlight-3</span> |
| <span style="padding: 3px; background: #1B1C2EFF; display: flex;">primary-4</span> | <span style="padding: 3px; background: #1C1D25FF; display: flex;">secondary-4</span> | <span style="padding: 3px; background: #0F2412FF; display: flex;">positive-4</span> | <span style="padding: 3px; background: #540709FF; display: flex;">negative-4</span> | <span style="padding: 3px; background: #44341DFF; display: flex;">highlight-4</span> |
| <span style="padding: 3px; background: #10111EFF; display: flex;">primary-5</span> | <span style="padding: 3px; background: #111217FF; display: flex;">secondary-5</span> | <span style="padding: 3px; background: #07170AFF; display: flex;">positive-5</span> | <span style="padding: 3px; background: #390204FF; display: flex;">negative-5</span> | <span style="padding: 3px; background: #2F2311FF; display: flex;">highlight-5</span> |

- A "root" in the VFS where all of your assets are, one per resource type (i.e. your textures and audio folders should be distinct roots.)
- An optional config type, wiring this up is left to the user.
### I really don't want a palette, though!
I can't really understand why you'd think this, but either way, inherit from BaseStylesheet instead of PalettedStylesheet. You'll lose functionality though, and need to bring your own sheetlets for everything.
You probably just want to come up with a palette (or a compat shim for this specific palette system) instead.
## Sheetlet
A sheetlet, or subsheet, is used to style a specific element or set of elements within its domain.
The primary usecase for sheetlets is to allow your element's stylesheet to be located in the same location as the element itself in the code.
Sheetlets marked CommonSheetlet, or otherwise gathered via GetAllSheetletRules, must not rely on rule order due to it being **undefined**.

Sheetlets that require special support from their associated stylesheet should provide an interface for that sheet to implement, and require the stylesheet load them directly. (For an example of this look at PalettedButtonSheetlet.)

# Design choices
## Palettes
There's no particular reason beyond "it's convenient" for any of the major choices surrounding palettes and their implementation.
This is a fancy way of saying "don't quote color theory at me, I swear to god.", I made it all up and it looks fine. For four-color things like buttons (which only have four states) the 4th palette entry is skipped so the 5th is used as the darkest color instead.
## Sheetlets instead of styling in XAML
Styling a control directly by setting properties in XAML effectively fixes the styling in place due to having higher specificity than any rule.
This is bad for a variety of reasons, the big one being that the UI's styling **cannot** be adjusted by the stylesheet if you do this.

Prefer using unique types, classes, and IDs. Style rules are implemented with similar cascading rules to Web CSS, so consult https://specifishity.com/ for a quick guide on how the engine determines which rules apply, think of directly setting properties as the equivalent to using `style=""` in HTML. RT has no `!important` equivalent by design.

### Wait controls have IDs?
yea it's the `StyleIdentifier` property. Matching on an ID will always have higher priority than a class or type selector. Use sparingly, prefer classes.

# TODO
move this to the docs site probably.
