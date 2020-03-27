# Tutorial and Examples

## Main concepts

Basic tgui backend code consists of the following vars and procs:

```
ui_interact(mob/user, ui_key, datum/tgui/ui, force_open,
  datum/tgui/master_ui, datum/ui_state/state)
ui_data(mob/user)
ui_act(action, params)
```

- `src_object` - The atom, which UI corresponds to in the game world.
- `ui_interact` - The proc where you will handle a request to open an
interface. Typically, you would update an existing UI (if it exists),
or set up a new instance of UI by calling the `SStgui` subsystem.
- `ui_data` - In this proc you munges whatever complex data your `src_object`
has into an associative list, which will then be sent to UI as a JSON string.
- `ui_act` - This proc receives user actions and reacts to them by changing
the state of the game.
- `ui_state` (set in `ui_interact`) - This var dictates under what conditions
a UI may be interacted with. This may be the standard checks that check if
you are in range and conscious, or more.

Once backend is complete, you create an new interface component on the
frontend, which will receive this JSON data and render it on screen.

States are easy to write and extend, and what make tgui interactions so
powerful. Because states can be overridden from other procs, you can build
powerful interactions for embedded objects or remote access.

## Using It

### Backend

Let's start with a very basic hello world.

```dm
/obj/machinery/my_machine/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = 0, datum/tgui/master_ui = null, datum/ui_state/state = default_state)
  ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
  if(!ui)
    ui = new(user, src, ui_key, "my_machine", name, 300, 300, master_ui, state)
    ui.open()
```

This is the proc that defines our interface. There's a bit going on here, so
let's break it down. First, we override the ui_interact proc on our object. This
will be called by `interact` for you, which is in turn called by `attack_hand`
(or `attack_self` for items). `ui_interact` is also called to update a UI (hence
the `try_update_ui`), so we accept an existing UI to update. The `state` is a
default argument so that a caller can overload it with named arguments
(`ui_interact(state = overloaded_state)`) if needed.

Inside the `if(!ui)` block (which means we are creating a new UI), we choose our
template, title, and size; we can also set various options like `style` (for
themes), or autoupdate. These options will be elaborated on later (as will
`ui_state`s).

After `ui_interact`, we need to define `ui_data`. This just returns a list of
data for our object to use. Let's imagine our object has a few vars:

```dm
/obj/machinery/my_machine/ui_data(mob/user)
  var/list/data = list()
  data["health"] = health
  data["color"] = color

  return data
```

The `ui_data` proc is what people often find the hardest about tgui, but its
really quite simple! You just need to represent your object as numbers, strings,
and lists, instead of atoms and datums.

Finally, the `ui_act` proc is called by the interface whenever the user used an
input. The input's `action` and `params` are passed to the proc.

```dm
/obj/machinery/my_machine/ui_act(action, params)
  if(..())
    return
  switch(action)
    if("change_color")
      var/new_color = params["color"]
      if(!(color in allowed_coors))
        return
      color = new_color
      . = TRUE
  update_icon()
```

The `..()` (parent call) is very important here, as it is how we check that the
user is allowed to use this interface (to avoid so-called href exploits). It is
also very important to clamp and sanitize all input here. Always assume the user
is attempting to exploit the game.

Also note the use of `. = TRUE` (or `FALSE`), which is used to notify the UI
that this input caused an update. This is especially important for UIs that do
not auto-update, as otherwise the user will never see their change.

### Frontend

Finally, you have to make a UI component. This is also a source of
confusion for many new users. If you got some basic javascript and HTML
knowledge, that should ease the learning process, although we recommend
getting yourself introduced to
[React and JSX](https://reactjs.org/docs/introducing-jsx.html).

A component is not a regular HTML. A component is a pure function, which
accepts a `props` object (it contains properties passed to a component),
and outputs an HTML-like structure consisting of regular HTML elements and
other UI components.

Interface component will always receive 1 prop which is called `state`.
This object contains a few special values:

- `config` is always the same and is part of core tgui
(it will be explained later),
- `data` is the data returned from `ui_data`

```jsx
import { Section, LabeledList } from '../components';

const SampleInterface = props => {
  const { state } = props;
  const { config, data } = state;
  const { ref } = config;
  return (
    <Section title="Health status">
      <LabeledList>
        <LabeledList.Item label="Health">
          {data.health}
        </LabeledList.Item>
        <LabeledList.Item label="Color">
          {data.color}
        </LabeledList.Item>
      </LabeledList>
    </Section>
  );
};
```

This syntax can be very confusing at first, but it is very important to
realize that this is just a natural extension of javascript. Here's a few
examples of this syntax:

Return a different element based on a condition:

```jsx
if (condition) {
  return <Foo />;
}
return <Bar />;
```

Conditionally render a element inside of another element:

```jsx
<Box>
  {showProgress && (
    <ProgressBar value={progress} />
  )}
</Box>
```

Looping over the array to make an element for each item:

```jsx
<LabeledList>
  {items.map(item => (
    <LabeledList.Item key={item.id} label={item.label}>
      {item.content}
    </LabeledList.Item>
  ))}
</LabeledList>
```

### Routing table

Once you finished creating your interface, you need to add a route entry to
the large `ROUTES` object, otherwise tgui won't know when and how to render
your interface. Key of this `ROUTES` object corresponds to the interface
name you use in DM code.

```js
import { SampleInterface } from './interfaces/SampleInterface';

const ROUTES = {
  sample_interface: {
    component: () => SampleInterface,
    scrollable: true,
  },
};
```

## Copypasta

We all do it, even the best of us. If you just want to make a tgui **fast**,
here's what you need (note that you'll probably be forced to clean your shit up
upon code review):

```dm
/obj/copypasta/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = 0, datum/tgui/master_ui = null, datum/ui_state/state = default_state) // Remember to use the appropriate state.
  ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
  if(!ui)
    ui = new(user, src, ui_key, "copypasta", name, 300, 300, master_ui, state)
    ui.open()

/obj/copypasta/ui_data(mob/user)
  var/list/data = list()
  data["var"] = var
  return data

/obj/copypasta/ui_act(action, params)
  if(..())
    return
  if(action == "copypasta")
    var/newvar = params["var"]
    // A demo of proper input sanitation.
    var = CLAMP(newvar, min_val, max_val)
    return TRUE
  update_icon() // Not applicable to all objects.
```

And the template:

```jsx
import { Section, LabeledList } from '../components';

const SampleInterface = props => {
  const { state } = props;
  const { config, data } = state;
  const { ref } = config;
  return (
    <Section title="Section name">
      <LabeledList>
        <LabeledList.Item label="Variable">
          {data.var}
        </LabeledList.Item>
      </LabeledList>
    </Section>
  );
};
```
