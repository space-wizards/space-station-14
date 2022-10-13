### Loc for the pneumatic cannon.

pneumatic-cannon-component-verb-gas-tank-name = Извлечь газовый баллон
pneumatic-cannon-component-verb-eject-items-name = Извлечь содержимое

## Shown when inserting items into it

pneumatic-cannon-component-insert-item-success = Вы помещаете { $item } в { $cannon }.
pneumatic-cannon-component-insert-item-failure = Похоже, что { $item } не помещается в { $cannon }.

## Shown when trying to fire, but no gas

pneumatic-cannon-component-fire-no-gas = { CAPITALIZE($cannon) } щёлкает, но газ не выходит.

## Shown when changing the fire mode or power.

pneumatic-cannon-component-change-fire-mode =
    { $mode ->
        [All] Вы ослабляете вентили, чтобы выстрелить всем сразу.
       *[Single] Вы затягиваете вентили, чтобы стрелять по одному предмету.
    }
pneumatic-cannon-component-change-power =
    { $power ->
        [High] Вы устанавливаете ограничитель на максимум. Как бы вышло не слишком сильно...
        [Medium] Вы устанавливаете ограничитель посередине.
       *[Low] Вы устанавливаете ограничитель на минимум.
    }

## Shown when inserting/removing the gas tank.

pneumatic-cannon-component-gas-tank-insert = Вы устанавливаете { $tank } в { $cannon }.
pneumatic-cannon-component-gas-tank-remove = Вы берёте { $tank } из { $cannon }.
pneumatic-cannon-component-gas-tank-none = В { $cannon } нет баллона!

## Shown when ejecting every item from the cannon using a verb.

pneumatic-cannon-component-ejected-all = Вы извлекаете всё из { $cannon }.

## Shown when being stunned by having the power too high.

pneumatic-cannon-component-power-stun = { CAPITALIZE($cannon) } сбивает вас с ног!
