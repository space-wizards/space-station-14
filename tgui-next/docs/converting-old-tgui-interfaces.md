# Converting old tgui interfaces to tgui-next

This guide is going to assume you already know roughly how tgui-next works, how to make new uis, etc. It's mostly aimed at helping translate concepts between tgui and tgui-next, and clarify some confusing parts of the transition.

## Backend

Backend in almost every case does not require any changes. In particularly heavy ui cases, something to be aware of is the new `ui_static_data()` proc. This proc allows you to split some data sent to the interface off into data that will only be sent on ui initialize and when manually updated by elsewhere in the code. Useful for things like cargo where you have a very large set of mostly identical code.

Keep in mind that for uis where *all* data doesn't need to be live updating, you can just toggle off autoupdate for the ui instead of messing with static data.

## Frontend

The very first thing to note is the name of the `ract` file containing the old interface. Whatever the name is (minus the extension) is going to be what the route key is going to be.

One thing I like to do before starting work on a conversion is screenshot what the old interface looks like so I have something to reference to make sure that the styling can line up as well.

## General syntax changes

Ractive has a fairly different templating syntax from React.

### `data`

You likely already know that React data inserts look like this

```jsx
{data.example_data}
```

Ractive looks very similar, the only real difference is that React uses one paranthesis instead of two.

```ractive
{{data.example_data}}
```

However, you may occasionally come across data inserts that instead of referencing the `data` var or things contained within it instead reference `adata`. `adata` was short for animated data, and was used for smooth number animations in interfaces. instead of having a seperate data structure for this. tgui-next instead uses a component, which is `AnimatedNumber`.

`AnimatedNumber` is used like this

```jsx
<AnimatedNumber value={data.example_data}/>
```

Make sure you don't forget to import it.

### Conditionals

Ractive conditionals look very different from React conditionals.

A ractive `if` (only render if result of expression is true) looks like this

```ractive
{{#if data.condition}}
  <span>Example Render</span>
{{/if}}
```

The equivalent React would be

```jsx
{!!data.condition && (
  <Fragment>Example Render</Fragment>
)}
```

This might look a bit intimidating compared to the reactive part but it's not as complicated as it seems:

1. A new jsx context is opened with `{}`
2. jsx contexts like this always render whatever the return value is, so we can use `&&` to return a value we want. `&&` returns the last true value (or not "falsey" because this is js).
3. jsx tags are never "falsey", so a conditioned paired with a jsx tag will mean the condition being true will continue on and return the tag. `()` is just used to contain the tag
4. The `!!` is not a special operator, it is a literal double negation. This is because most `false` values coming from byond are going to actually be `0`, which would be rendered if the condition is false. Negating `0` returns `true`, negating `true` returns `false`, which isn't rendered.
5. `Fragment` is actually a true "dead tag". It's similar to `span` in that it just contains things without providing functionality, but it's unwrapped before the final render and children of it are injected into its parent. In a case where you only need to render text without any styling, it's probably better to just return a string literal (`"Example Render"`), but this was just to illustrate that you can put any tag in this expression.

You don't really need to know all this to understand how to use it, but I find it helps with understanding when things go wrong.

Ractive conditionals can have an `else` as well
```ractive
{{#if data.condition}}
  value
{{else}}
  other value
{{/if}}
```

Similarly to the previous example, just add a `||` operator to handle the
"falsy" condition:

```jsx
{!!data.condition && (
  <Fragment>value</Fragment>
) || (
  <Fragment>other value</Fragment>
)}
```

There's also our good old friend - the ternary:

```jsx
{data.condition ? 'value' : 'other value'}
```

Keep in mind you can also use tags here like the conditional example,
and you can mix string literals, values, and tags as well.

```jsx
{data.is_robot ? (
  <Button content="Robot Button"/>
) : 'Not a robot'}
```

### Loops

Ractive has loops for iterating over data and inserting something for each
member of an array or object

```
{{#each data.list_of_foo}}
  foo {{number}} is here.
{{/each}}
```

This didn't care whether the data was an array or an object, and members of each entry of the loop were "unwrapped" so to say. `{{number}}` in that example is referring to the `{{number}}` value on the entry of the list for that iterate.

The React equivalent to this is going to be `map`.

_AN IMPORTANT DISTINCTION HERE IS THAT NOW WE CARE WHETHER THIS IS AN OBJECT OR AN ARRAY BEING ACTED ON._

Objects are represented by `{}`, arrays by `[]`

"How can I tell?" you may ask. It's fairly simple, associated lists on the byond side are going to be turned into objects when they get json converted, normal lists are going to be turned into arrays.

`list("bla", "blo")` would become `["bla", "blo"]` and `list("foo" = 1, "bar" = 2)` would become `{"foo": 1, "bar": 2}`

First things first, above the `return` of the function you're making the interface in, you're going to want to add something like this
```jsx
const things = data.things || [];
```

This ensures that you'll never be reading a null entry by mistake. Substitute `{}` for objects as appropriate.

If it's an array, you'll want to do this in the template
```jsx
{things.map(thing => (
  <Fragment>Thing {thing.number} is here!</Fragment>
))}
```

`map` is a function that calls a passed function (a lambda) on each entry, and returns the value. You should already know that returned tags and values (except `false`) get rendered, so that's how it's rendering each time.

A lambda is what's known as an anonymous function, it's a function that doesn't have a name that's only used for a specific usage. `map` wants a function that has one parameter, so we define one parameter then use `=>` to say the parameter has to do with the following block.

`parameter => ()` is just a shorthand for `parameter => {return();}`

This is quite a bit higher concept than ractive's each statements, so feel free to look around and ~~copy paste~~ learn from how other interfaces use this.

Now for objects, there's a genuinely pretty gross syntax here. We apoligize, it's related to ie8 compatibility nonsense.

```jsx
{map((value, key) => {
  return (
    <Fragment>Key is {key}, value is {value}</Fragment>
  );
})(fooObject)}
```

Again, sorry for this syntax. `fooObject` would be the object being iterated on, value would be the value of the iterated entry on the list, and key would be the key. the naming of value and key isn't important here, but knowing that it goes `value`, `key` in that order is important.

It is sometimes better to preemptively convert an object to array before
the big return statement, like this:

```jsx
const fooArray = map((value, key) => {
  return { key, value };
})(fooObject);
```

Or if you just want to discard all keys, this will also work nicely:

```jsx
const fooArray = toArray(fooObject);
```

Also occasionally you'd see an else:

```
{{#each data.potentially_empty_list}}
  Thing "{{name}}" is in this list!
{{else}}
  None found!
{{/each}}
```

This would iterate using the first contents each time, or display the second option if the list was empty.

To do a similar thing in JSX, just check if array is empty like this:

```jsx
{fooArray.length === 0 && 'fooArray is empty.'}
{fooArray.map(foo => (
  <Fragment>Foo is {foo}</Fragment>
))}
```

### Extra Stuff

I'll put some extra stuff here when I think of it.

## Components

This will be a reference of tgui components and the tgui-next equivalent.

### `ui-display`

Equivalent of `<ui-display>` is `<Section>`

```
<ui-display title="Status">
  Contents
</ui-display>
```

becomes

```jsx
<Section title="Status">
  Contents
</Section>
```

A feature sometimes used is if `ui-display` has the `button` property, it will contain a `partial` command. This becomes the `buttons` property on `Section`:

```
<ui-display title="Status" button>
  {{#partial button}}
    <ui-button /> // lots more button bullshit here
  {{/partial}}
  Contents
</ui-display>
```

becomes

```jsx
<Section
  title="Status"
  buttons={(
    <Button />
  )}>
  Contents
</Section>
```

### `ui-section`

Very important to note `ui-section` is NOT the equivalent of `Section`

`<ui-section>` does not have a direct equivalent, but the closest equivalent is `<LabeledList>`

```
<ui-section label="power">
  No Power
</ui-section>
<ui-section label="connection">
  No Connection
</ui-section>
```

becomes

```jsx
<LabeledList>
  <LabeledList.Item label="power">
    No Power
  </LabeledList.Item>
  <LabeledList.Item label="connection">
    No Connection
  </LabeledList.Item>
</LabeledList>
```

Important to note that `LabeledList.Item` has `buttons` as well.

Also good to know that if you need the contents of a `LabeledList.Item` to be colored, you can just set the `color` prop on it instead of putting a `span` inside it.

### `ui-notice`

`<ui-notice>` has a direct equivalent in `<NoticeBox>`

```
<ui-notice>
  Notice stuff!
</ui-notice>
```

becomes

```jsx
<NoticeBox>
  Notice stuff!
</NoticeBox>
```

### `ui-button`

The equivalent of `ui-button` is `Button` but it works quite a bit differently.

```
<ui-button
  state='{{data.condition ? "disabled" : null}}'
  action="ui_action"
  params={param: value}>
  Click
</ui-button>
```

becomes

```
<Button
  content="Click"
  disabled={data.condition}
  onClick={() => act(ref, "ui_action", {param: value})}/>
```
