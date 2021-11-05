### Loc for the pneumatic cannon.

pneumatic-cannon-component-verb-gas-tank-name = Eject gas tank
pneumatic-cannon-component-verb-eject-items-name = Eject all items

## Shown when inserting items into it

pneumatic-cannon-component-insert-item-success = You insert { THE($item) } into { THE($cannon) }.
pneumatic-cannon-component-insert-item-failure = You can't seem to fit { THE($item) } in { THE($cannon) }.

## Shown when trying to fire, but no gas

pneumatic-cannon-component-fire-no-gas = { CAPITALIZE(THE($cannon)) } clicks, but no gas comes out.

## Shown when changing the fire mode or power.

pneumatic-cannon-component-change-fire-mode = { $mode ->
    [All] You loosen the valves to fire everything at once.
    *[Single] You tighten the valves to fire one item at a time.
}

pneumatic-cannon-component-change-power = { $power ->
    [High] You set the limiter to maximum power. It feels a little too powerful...
    [Medium] You set the limiter to medium power.
    *[Low] You set the limiter to low power.
}

## Shown when inserting/removing the gas tank.

pneumatic-cannon-component-gas-tank-insert = You fit { THE($tank) } onto { THE($cannon) }.
pneumatic-cannon-component-gas-tank-remove = You take { THE($tank) } off of { THE($cannon) }.
pneumatic-cannon-component-gas-tank-none = There is no gas tank on { THE($cannon) }!

## Shown when ejecting every item from the cannon using a verb.

pneumatic-cannon-component-ejected-all = You eject everything from { THE($cannon) }.

## Shown when being stunned by having the power too high.

pneumatic-cannon-component-power-stun = The pure force of { THE($cannon) } knocks you over!

