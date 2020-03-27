# tgui

## Introduction

tgui is a robust user interface framework of /tg/station.

tgui is very different from most UIs you will encounter in BYOND programming.
It is heavily reliant on Javascript and web technologies as opposed to DM.
If you are familiar with NanoUI (a library which can be found on almost
every other SS13 codebase), tgui should be fairly easy to pick up.

## Learn tgui

People come to tgui from different backgrounds and with different
learning styles. Whether you prefer a more theoretical or a practical
approach, we hope you’ll find this section helpful.

### Practical tutorial

If you are completely new to frontend and prefer to **learn by doing**,
start with our [practical tutorial](docs/tutorial-and-examples.md).

### Guides

This project uses **Inferno** - a very fast UI rendering engine with a similar
API to React. Take your time to read these guides:

- [React guide](https://reactjs.org/docs/hello-world.html)
- [Inferno documentation](https://infernojs.org/docs/guides/components) -
highlights differences with React.

If you were already familiar with an older, Ractive-based tgui, and want
to translate concepts between old and new tgui, read this
[interface conversion guide](docs/converting-old-tgui-interfaces.md).

## Pre-requisites

You will need these programs to start developing in tgui:

- [Node v12.13+](https://nodejs.org/en/download/)
- [Yarn v1.19+](https://yarnpkg.com/en/docs/install)
- [MSys2](https://www.msys2.org/) (optional)

> MSys2 closely replicates a unix-like environment which is necessary for
> the `bin/tgui` script to run. It comes with a robust "mintty" terminal
> emulator which is better than any standard Windows shell, it supports
> "git" out of the box (almost like Git for Windows, but better), has
> a "pacman" package manager, and you can install a text editor like "vim"
> for a full boomer experience.

## Usage

**For MSys2, Git Bash, WSL, Linux or macOS users:**

First and foremost, change your directory to `tgui-next`.

Run `bin/tgui --install-git-hooks` (optional) to install merge drivers
which will assist you in conflict resolution when rebasing your branches.

Run one of the following:

- `bin/tgui` - build the project in production mode.
- `bin/tgui --dev` - launch a development server.
  - tgui development server provides you with incremental compilation,
  hot module replacement and logging facilities in all running instances
  of tgui. In short, this means that you will instantly see changes in the
  game as you code it. Very useful, highly recommended.
  In order to use, you should start the game server first, connect to it so dreamseeker is
  open, then start the dev server. You'll know if it's hooked correctly if data gets dumped
  to the log when tgui windows are opened.
- `bin/tgui --dev --reload` - reload byond cache once.
- `bin/tgui --dev --debug` - run server with debug logging enabled.
- `bin/tgui --dev --no-hot` - disable hot module replacement (helps when
doing development on IE8).
- `bin/tgui --lint` - show problems with the code.
- `bin/tgui --lint --fix` - auto-fix problems with the code.
- `bin/tgui --analyze` - run a bundle analyzer.
- `bin/tgui --clean` - clean up project repo.
- `bin/tgui [webpack options]` - build the project with custom webpack
options.

**For everyone else:**

If you haven't opened the console already, you can do that by holding
Shift and right clicking on the `tgui-next` folder, then pressing
either `Open command window here` or `Open PowerShell window here`.

Run `yarn install` to install npm dependencies, then one of the following:

- `yarn run build` - build the project in production mode.
- `yarn run watch` - launch a development server.
- `yarn run lint` - show problems with the code.
- `yarn run lint --fix` - auto-fix problems with the code.
- `yarn run analyze` - run a bundle analyzer.

We also got some batch files in store, for those who don't like fiddling
with the console:

- `bin/tgui-build.bat` - build the project in production mode.
- `bin/tgui-dev-server.bat` - launch a development server.

> Remember to always run a full build before submitting a PR. It creates
> a compressed javascript bundle which is then referenced from DM code.
> We prefer to keep it version controlled, so that people could build the
> game just by using Dream Maker.

## Project structure

- `/packages` - Each folder here represents a self-contained Node module.
- `/packages/common` - Helper functions
- `/packages/tgui/index.js` - Application entry point.
- `/packages/tgui/components` - Basic UI building blocks.
- `/packages/tgui/interfaces` - Actual in-game interfaces.
Interface takes data via the `state` prop and outputs an html-like stucture,
which you can build using existing UI components.
- `/packages/tgui/routes.js` - This is where you want to register new
interfaces, otherwise they simply won't load.
- `/packages/tgui/layout.js` - A root-level component, holding the
window elements, like the titlebar, buttons, resize handlers. Calls
`routes.js` to decide which component to render.
- `/packages/tgui/styles/main.scss` - CSS entry point.
- `/packages/tgui/styles/atomic.scss` - Atomic CSS classes.
These are very simple, tiny, reusable CSS classes which you can use and
combine to change appearance of your elements. Keep them small.
- `/packages/tgui/styles/components.scss` - CSS classes which are used
in UI components, and most of the stylesheets referenced here are located
in `/packages/tgui/components`. These stylesheets closely follow the
[BEM](https://en.bem.info/methodology/) methodology.
- `/packages/tgui/styles/functions.scss` - Useful SASS functions.
Stuff like `lighten`, `darken`, `luminance` are defined here.

## Component reference

> Notice: This documentation might be out of date, so always check the source
> code to see the most up-to-date information.

These are the components which you can use for interface construction.
If you have trouble finding the exact prop you need on a component,
please note, that most of these components inherit from other basic
components, such as `Box`. This component in particular provides a lot
of styling options for all components, e.g. `color` and `opacity`, thus
it is used a lot in this framework.

There are a few important semantics you need to know about:

- `content` prop is a synonym to a `children` prop.
  - `content` is better used when your element is a self-closing tag
  (like `<Button content="Hello" />`), and when content is small and simple
  enough to fit in a prop. Keep in mind, that this prop is **not** native
  to React, and is a feature of this component system.
  - `children` is better used when your element is a full tag (like
  `<Button>Hello</Button>`), and when content is long and complex. This is
  a native React prop (unlike `content`), and contains all elements you
  defined between the opening and the closing tag of an element.
  - You should never use both on a same element.
  - You should never use `children` explicitly as a prop on an element.
- Inferno supports both camelcase (`onClick`) and lowercase (`onclick`)
event names.
  - Camel case names are what's called "synthetic" events, and are the
  *preferred way* of handling events in React, for efficiency and
  performance reasons. Please read
  [Inferno Event Handling](https://infernojs.org/docs/guides/event-handling)
  to understand what this is about.
  - Lower case names are native browser events and should be used sparingly,
  for example when you need an explicit IE8 support. **DO NOT** use
  lowercase event handlers unless you really know what you are doing.
  - [Button](#button) component straight up does not support lowercase event
  handlers. Use the camel case `onClick` instead.

### `AnimatedNumber`

This component provides animations for numeric values.

Props:

- `value: number` - Value to animate.
- `initial: number` - Initial value to use in animation when element
first appears. If you set initial to `0` for example, number will always
animate starting from `0`, and if omitted, it will not play an initial
animation.
- `format: value => value` - Output formatter.
  - Example: `value => Math.round(value)`.
- `children: (formattedValue, rawValue) => any` - Pull the animated number to
animate more complex things deeper in the DOM tree.
  - Example: `(_, value) => <Icon rotation={value} />`

### `BlockQuote`

Just a block quote, just like this example in markdown:

> Here's an example of a block quote.

Props:

- See inherited props: [Box](#box)

### `Box`

The Box component serves as a wrapper component for most of the CSS utility
needs. It creates a new DOM element, a `<div>` by default that can be changed
with the `as` property. Let's say you want to use a `<span>` instead:

```jsx
<Box as="span" m={1}>
  <Button />
</Box>
```

This works great when the changes can be isolated to a new DOM element.
For instance, you can change the margin this way.

However, sometimes you have to target the underlying DOM element.
For instance, you want to change the text color of the button. The Button
component defines its own color. CSS inheritance doesn't help.

To workaround this problem, the Box children accept a render props function.
This way, `Button` can pull out the `className` generated by the `Box`.

```jsx
<Box color="primary">
  {props => <Button {...props} />}
</Box>
```

`Box` units, like width, height and margins can be defined in two ways:
- By plain numbers (1 unit equals `0.5em`);
- In absolute measures, by providing a full unit string (e.g. `100px`).

Units which are used in `Box` are `0.5em`, which are half font-size.
Default font size is `12px`, so each unit is effectively `6px` in size.
If you need more precision, you can always use fractional numbers.

Props:

- `as: string` - The component used for the root node.
- `color: string` - Applies an atomic `color-<name>` class to the element.
  - See `styles/atomic/color.scss`.
- `width: number` - Box width.
- `minWidth: number` - Box minimum width.
- `maxWidth: number` - Box maximum width.
- `height: number` - Box height.
- `minHeight: number` - Box minimum height.
- `maxHeight: number` - Box maximum height.
- `fontSize: number` - Font size.
- `fontFamily: string` - Font family.
- `lineHeight: number` - Directly affects the height of text lines.
Useful for adjusting button height.
- `inline: boolean` - Forces the `Box` to appear as an `inline-block`,
or in other words, makes the `Box` flow with the text instead of taking
all available horizontal space.
- `m: number` - Margin on all sides.
- `mx: number` - Horizontal margin.
- `my: number` - Vertical margin.
- `mt: number` - Top margin.
- `mb: number` - Bottom margin.
- `ml: number` - Left margin.
- `mr: number` - Right margin.
- `opacity: number` - Opacity, from 0 to 1.
- `bold: boolean` - Make text bold.
- `italic: boolean` - Make text italic.
- `nowrap: boolean` - Stops text from wrapping.
- `textAlign: string` - Align text inside the box.
  - `left` (default)
  - `center`
  - `right`
- `position: string` - A direct mapping to `position` CSS property.
  - `relative` - Relative positioning.
  - `absolute` - Absolute positioning.
  - `fixed` - Fixed positioning.
- `color: string` - An alias to `textColor`.
- `textColor: string` - Sets text color.
  - `#ffffff` - Hex format
  - `rgba(255, 255, 255, 1)` - RGB format
  - `purple` - Applies an atomic `color-<name>` class to the element.
  See `styles/color-map.scss`.
- `backgroundColor: string` - Sets background color.
  - `#ffffff` - Hex format
  - `rgba(255, 255, 255, 1)` - RGB format

### `Button`

Buttons allow users to take actions, and make choices, with a single click.

Props:

- See inherited props: [Box](#box)
- `fluid: boolean` - Fill all available horizontal space.
- `icon: string` - Adds an icon to the button.
- `color: string` - Button color, as defined in `variables.scss`.
  - There is also a special color `transparent` - makes the button
  transparent and slightly dim when inactive.
- `disabled: boolean` - Disables and greys out the button.
- `selected: boolean` - Activates the button (gives it a green color).
- `tooltip: string` - A fancy, boxy tooltip, which appears when hovering
over the button.
- `tooltipPosition: string` - Position of the tooltip.
  - `top` - Show tooltip above the button.
  - `bottom` (default) - Show tooltip below the button.
  - `left` - Show tooltip on the left of the button.
  - `right` - Show tooltip on the right of the button.
- `ellipsis: boolean` - If button width is constrained, button text will
be truncated with an ellipsis. Be careful however, because this prop breaks
the baseline alignment.
- `title: string` - A native browser tooltip, which appears when hovering
over the button.
- `content/children: any` - Content to render inside the button.
- `onClick: function` - Called when element is clicked.

### `Button.Checkbox`

A ghetto checkbox, made entirely using existing Button API.

Props:

- See inherited props: [Button](#button)
- `checked: boolean` - Boolean value, which marks the checkbox as checked.

### `Button.Confirm`

A button with a an extra confirmation step, using native button component.

Props:

- See inherited props: [Button](#button)
- `confirmMessage: string` - Text to display after first click; defaults to "Confirm?"
- `confirmColor: string` - Color to display after first click; default to "bad"

### `Button.Input`

A button that turns into an input box after the first click. Turns back into a button after the user hits enter, defocuses, or hits escape. Enter and defocus commit, while escape cancels.

Props:
 - See inherited props: [Box](#box)
 - `fluid`: fill availible horizontal space
 - `onCommit: (e, value) => void`: function that is called after the user defocuses the input or presses enter
 - `currentValue: string`: default string to display when the input is shown
 - `defaultValue: string`: default value emitted if the user leaves the box blank when hitting enter or defocusing. If left undefined, will cancel the change on a blank defocus/enter

### `Collapsible`

Displays contents when open, acts as a fluid button when closed. Click to toggle, closed by default.

Props:
  - See inherited props: [Box](#box)
  - `children: any` - What is collapsed when closed
  - `title: string` - Text to display on the button for collapsing
  - `color: string` - Color of the button; see [Button](#button)
  - `buttons: any` - Buttons or other content to render inline with the button

### `ColorBox`

Displays a 1-character wide colored square. Can be used as a status indicator,
or for visually representing a color.

If you want to set a background color on an element, use a plain
[Box](#box) instead.

Props:

- See inherited props: [Box](#box)
- `color: string` - Color of the box.

### `Dimmer`

Dims surrounding area to emphasize content placed inside.

Props:

- See inherited props: [Box](#box)

### `Dropdown`

A simple dropdown box component. Lets the user select from a list of options and displays selected entry.

Props:

  - See inherited props: [Box](#box)
  - `options: string[]` - An array of strings which will be displayed in the dropdown when open
  - `selected: string` - Currently selected entry
  - `width: number` - Width of dropdown button and resulting menu
  - `over: boolean` - dropdown renders over instead of below
  - `color: string` - color of dropdown button
  - `onClick: (e) => void` - Called when dropdown button is clicked
  - `onSelected: (value) => void` - Called when a value is picked from the list, `value` is the value that was picked

### `Flex`

Quickly manage the layout, alignment, and sizing of grid columns, navigation, components, and more with a full suite of responsive flexbox utilities.

If you are new to or unfamiliar with flexbox, we encourage you to read this
[CSS-Tricks flexbox guide](https://css-tricks.com/snippets/css/a-guide-to-flexbox/).

Consists of two elements: `<Flex>` and `<Flex.Item>`. Both of them provide
the most straight-forward mapping to flex CSS properties as possible.

One of the most basic usage of flex, is to align certain elements
to the left, and certain elements to the right:

```jsx
<Flex>
  <Flex.Item>
    Button description
  </Flex.Item>
  <Flex.Item grow={1} />
  <Flex.Item>
    <Button content="Perform an action" />
  </Flex.Item>
</Flex>
```

Flex item with `grow` property serves as a "filler", to separate the other
two flex items as far as possible from each other.

Props:

- See inherited props: [Box](#box)
- `spacing: number` - Spacing between flex items, in integer units
(1 unit - 0.5em). Does not directly relate to a flex css property
(adds a modifier class under the hood), and only integer numbers are
supported.
- `direction: string` - This establishes the main-axis, thus defining the
direction flex items are placed in the flex container.
  - `row` (default) - left to right.
  - `row-reverse` - right to left.
  - `column` - top to bottom.
  - `column-reverse` - bottom to top.
- `wrap: string` - By default, flex items will all try to fit onto one line.
You can change that and allow the items to wrap as needed with this property.
  - `nowrap` (default) - all flex items will be on one line
  - `wrap` - flex items will wrap onto multiple lines, from top to bottom.
  - `wrap-reverse` - flex items will wrap onto multiple lines from bottom to top.
- `align: string` - Default alignment of all children.
  - `stretch` (default) - stretch to fill the container.
  - `start` - items are placed at the start of the cross axis.
  - `end` - items are placed at the end of the cross axis.
  - `center` - items are centered on the cross axis.
  - `baseline` - items are aligned such as their baselines align.
- `justify: string` - This defines the alignment along the main axis.
It helps distribute extra free space leftover when either all the flex
items on a line are inflexible, or are flexible but have reached their
maximum size. It also exerts some control over the alignment of items
when they overflow the line.
  - `flex-start` (default) - items are packed toward the start of the
  flex-direction.
  - `flex-end` - items are packed toward the end of the flex-direction.
  - `space-between` - items are evenly distributed in the line; first item is
  on the start line, last item on the end line
  - `space-around` - items are evenly distributed in the line with equal space
  around them. Note that visually the spaces aren't equal, since all the items
  have equal space on both sides. The first item will have one unit of space
  against the container edge, but two units of space between the next item
  because that next item has its own spacing that applies.
  - `space-evenly` - items are distributed so that the spacing between any two
  items (and the space to the edges) is equal.
  - TBD (not all properties are supported in IE11).

### `Flex.Item`

Props:

- See inherited props: [Box](#box)
- `order: number` - By default, flex items are laid out in the source order.
However, the order property controls the order in which they appear in the
flex container.
- `grow: number` - This defines the ability for a flex item to grow if
necessary. It accepts a unitless value that serves as a proportion. It
dictates what amount of the available space inside the flex container the
item should take up. This number is unit-less and is relative to other
siblings.
- `shrink: number` - This defines the ability for a flex item to shrink
if necessary. Inverse of `grow`.
- `basis: string` - This defines the default size of an element before the
remaining space is distributed. It can be a length (e.g. `20%`, `5rem`, etc.),
an `auto` or `content` keyword.
- `align: string` - This allows the default alignment (or the one specified by align-items) to be overridden for individual flex items. See: [Flex](#flex).


### `Grid`

Helps you to divide horizontal space into two or more equal sections.
It is essentially a single-row `Table`, but with some extra features.

Example:

```jsx
<Grid>
  <Grid.Column>
    <Section title="Section 1" content="Hello world!" />
  </Grid.Column>
  <Grid.Column size={2}>
    <Section title="Section 2" content="Hello world!" />
  </Grid.Column>
</Grid>
```

Props:

- See inherited props: [Table](#table)

### `Grid.Column`

Props:

- See inherited props: [Table.Cell](#tablecell)
- `size: number` (default: 1) - Size of the column relative to other columns.

### `Icon`

Renders one of the FontAwesome icons of your choice.

```jsx
<Icon name="plus" />
```

To smoothen the transition from v4 to v5, we have added a v4 semantic to
transform names with `-o` suffixes to FA Regular icons. For example:
- `square` will get transformed to `fas square`
- `square-o` will get transformed to `far square`

Props:

- See inherited props: [Box](#box)
- `name: string` - Icon name.
- `size: number` - Icon size. `1` is normal size, `2` is two times bigger.
Fractional numbers are supported.
- `rotation: number` - Icon rotation, in degrees.
- `spin: boolean` - Whether an icon should be spinning. Good for load
indicators.

### `Input`

A basic text input, which allow users to enter text into a UI.

> Input does not support custom font size and height due to the way
> it's implemented in CSS. Eventually, this needs to be fixed.

Props:

- See inherited props: [Box](#box)
- `value: string` - Value of an input.
- `placeholder: string` - Text placed into Input box when value is otherwise nothing. Clears automatically when focused.
- `fluid: boolean` - Fill all available horizontal space.
- `selfClear: boolean` - Clear after hitting enter, as well as remain focused when this happens. Useful for things like chat inputs
- `onChange: (e, value) => void` - An event, which fires when you commit
the text by either unfocusing the input box, or by pressing the Enter key.
- `onInput: (e, value) => void` - An event, which fires on every keypress.

### `LabeledList`

LabeledList is a continuous, vertical list of text and other content, where
every item is labeled. It works just like a two column table, where first
column is labels, and second column is content.

```jsx
<LabeledList>
  <LabeledList.Item label="Item">
    Content
  </LabeledList.Item>
</LabeledList>
```

If you want to have a button on the right side of an item (for example,
to perform some sort of action), there is a way to do that:

```jsx
<LabeledList>
  <LabeledList.Item
    label="Item"
    buttons={(
      <Button content="Click me!" />
    )}>
    Content
  </LabeledList.Item>
</LabeledList>
```

Props:

- `children: LabeledList.Item` - Items to render.

### `LabeledList.Item`

Props:

- `label: string` - Item label.
- `color: string` - Sets the color of the text.
- `buttons: any` - Buttons to render aside the content.
- `content/children: any` - Content of this labeled item.

### `LabeledList.Divider`

Adds some empty space between LabeledList items.

Example:

```jsx
<LabeledList>
  <LabeledList.Item label="Foo">
    Content
  </LabeledList.Item>
  <LabeledList.Divider size={1} />
</LabeledList>
```

Props:

- `size: number` - Size of the divider.

### `NoticeBox`

A notice box, which warns you about something very important.

Props:

- See inherited props: [Box](#box)

### `NumberInput`

A fancy, interactive number input, which you can either drag up and down
to fine tune the value, or single click it to manually type a number.

Props:

- `animated: boolean` - Animates the value if it was changed externally.
- `fluid: boolean` - Fill all available horizontal space.
- `value: number` - Value itself.
- `unit: string` - Unit to display to the right of value.
- `minValue: number` - Lowest possible value.
- `maxValue: number` - Highest possible value.
- `step: number` (default: 1) - Adjust value by this amount when
dragging the input.
- `stepPixelSize: number` (default: 1) - Screen distance mouse needs
to travel to adjust value by one `step`.
- `width: string|number` - Width of the element, in `Box` units or pixels.
- `height: string|numer` - Height of the element, in `Box` units or pixels.
- `lineHeight: string|number` - lineHeight of the element, in `Box` units or pixels.
- `fontSize: string|number` - fontSize of the element, in `Box` units or pixels.
- `format: value => value` - Format value using this function before
displaying it.
- `suppressFlicker: number` - A number in milliseconds, for which the input
will hold off from updating while events propagate through the backend.
Default is about 250ms, increase it if you still see flickering.
- `onChange: (e, value) => void` - An event, which fires when you release
the input, or successfully enter a number.
- `onDrag: (e, value) => void` - An event, which fires about every 500ms
when you drag the input up and down, on release and on manual editing.

### `ProgressBar`

Progress indicators inform users about the status of ongoing processes.

```jsx
<ProgressBar value={0.6} />
```

Usage of `ranges` prop:

```jsx
<ProgressBar
  ranges={{
    good: [0.5, Infinity],
    average: [0.25, 0.5],
    bad: [-Infinity, 0.25],
  }}
  value={0.6} />
```

Props:

- `value: number` - Current progress as a floating point number between
`minValue` (default: 0) and `maxValue` (default: 1). Determines the
percentage and how filled the bar is.
- `minValue: number` - Lowest possible value.
- `maxValue: number` - Highest possible value.
- `ranges: { color: [from, to] }` - Applies a `color` to the progress bar
based on whether the value lands in the range between `from` and `to`.
- `color: string` - Color of the progress bar.
- `content/children: any` - Content to render inside the progress bar.

### `Section`

Section is a surface that displays content and actions on a single topic.

They should be easy to scan for relevant and actionable information.
Elements, like text and images, should be placed in them in a way that
clearly indicates hierarchy.

Section can also be titled to clearly define its purpose.

```jsx
<Section title="Cargo">
  Here you can order supply crates.
</Section>
```

If you want to have a button on the right side of an section title
(for example, to perform some sort of action), there is a way to do that:

```jsx
<Section
  title="Cargo"
  buttons={(
    <Button content="Send shuttle" />
  )}>
  Here you can order supply crates.
</Section>
```

- See inherited props: [Box](#box)
- `title: string` - Title of the section.
- `level: number` - Section level in hierarchy. Default is 1, higher number
means deeper level of nesting. Must be an integer number.
- `buttons: any` - Buttons to render aside the section title.
- `content/children: any` - Content of this section.

### `Table`

A straight forward mapping to a standard html table, which is slightly
simplified (does not need a `<tbody>` tag) and with sane default styles
(e.g. table width is 100% by default).

Example:

```jsx
<Table>
  <Table.Row>
    <Table.Cell bold>
      Hello world!
    </Table.Cell>
    <Table.Cell collapsing color="label">
      Label
    </Table.Cell>
  </Table.Row>
</Table>
```

Props:

- See inherited props: [Box](#box)
- `collapsing: boolean` - Collapses table to the smallest possible size.

### `Table.Row`

A straight forward mapping to `<tr>` element.

Props:

- See inherited props: [Box](#box)

### `Table.Cell`

A straight forward mapping to `<td>` element.

Props:

- See inherited props: [Box](#box)
- `collapsing: boolean` - Collapses table cell to the smallest possible size,
and stops any text inside from wrapping.

### `Tabs`

Tabs make it easy to explore and switch between different views.

Here is an example of how you would construct a simple tabbed view:

```jsx
<Tabs>
  <Tabs.Tab label="Item one">
    Content for Item one.
  </Tabs.Tab>
  <Tabs.Tab label="Item two">
    Content for Item two.
  </Tabs.Tab>
</Tabs>
```

This is a rather simple example. In the real world, you might be
constructing very complex tabbed views which can tax UI performance.
This is because your tabs are being rendered regardless of their
visibility status!

There is a simple fix however. Tabs accept functions as children, which
will be called to retrieve content only when the tab is visible:

```jsx
<Tabs>
  <Tabs.Tab key="tab_1" label="Item one">
    {() => (
      <Fragment>
        Content for Item one.
      </Fragment>
    )}
  </Tabs.Tab>
  <Tabs.Tab key="tab_2" label="Item two">
    {() => (
      <Fragment>
        Content for Item two.
      </Fragment>
    )}
  </Tabs.Tab>
</Tabs>
```

You might not always need this, but it is highly recommended to always
use this method. Notice the `key` prop on tabs - it uniquely identifies
the tab and is used for determining which tab is currently active. It can
be either explicitly provided as a `key` prop, or if omitted, it will be
implicitly derived from the tab's `label` prop.

Props:

- `vertical: boolean` - Use a vertical configuration, where tabs will appear
stacked on the left side of the container.
- `children: Tab[]` - This component only accepts tabs as its children.

### `Tabs.Tab`

An individual tab element. Tabs function like buttons, so they inherit
a lot of `Button` props.

Props:

- See inherited props: [Button](#button)
- `key: string` - A unique identifier for the tab.
- `label: string` - Tab label.
- `icon: string` - Tab icon.
- `content/children: any` - Content to render inside the tab.
- `onClick: function` - Called when element is clicked.

### `Tooltip`

A boxy tooltip from tgui 1. It is very hacky in its current state, and
requires setting `position: relative` on the container.

Please note, that [Button](#button) component has a `tooltip` prop, and
it is recommended to use that prop instead.

Usage:

```jsx
<Box position="relative">
  Sample text.
  <Tooltip
    position="bottom"
    content="Box tooltip" />
</Box>
```

Props:

- `position: string` - Tooltip position.
- `content/children: string` - Content of the tooltip. Must be a plain string.
Fragments or other elements are **not** supported.
