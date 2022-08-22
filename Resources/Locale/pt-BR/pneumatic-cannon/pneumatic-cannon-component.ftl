### Loc for the pneumatic cannon.

pneumatic-cannon-component-verb-gas-tank-name = Ejetar tanque de gás
pneumatic-cannon-component-verb-eject-items-name = Ejetar tudo

## Shown when inserting items into it

pneumatic-cannon-component-insert-item-success = Você insere { THE($item) } no { THE($cannon) }.
pneumatic-cannon-component-insert-item-failure = Você não consegue encaixar o(a) { THE($item) } no { THE($cannon) }.

## Shown when trying to fire, but no gas

pneumatic-cannon-component-fire-no-gas = { CAPITALIZE(THE($cannon)) } estala, mas nenhum gás sai.

## Shown when changing the fire mode or power.

pneumatic-cannon-component-change-fire-mode = { $mode ->
    [All] Você solta as válvulas para soltar tudo de uma vez.
    *[Single] Você aperta as válvulas soltar uma coisa de cada vez.
}

pneumatic-cannon-component-change-power = { $power ->
    [High] Você seleciona o limitador para energia alta. parece estar muito energizado...
    [Medium] Você seleciona o limitador para energia média.
    *[Low] Você seleciona o limitador para energia baixa.
}

## Shown when inserting/removing the gas tank.

pneumatic-cannon-component-gas-tank-insert = Você encaixa um { THE($tank) } no { THE($cannon) }.
pneumatic-cannon-component-gas-tank-remove = Você tira um { THE($tank) } do { THE($cannon) }.
pneumatic-cannon-component-gas-tank-none = Não há um tanque de gás no { THE($cannon) }!

## Shown when ejecting every item from the cannon using a verb.

pneumatic-cannon-component-ejected-all = Você ejeta tudo do(a) { THE($cannon) }.

## Shown when being stunned by having the power too high.

pneumatic-cannon-component-power-stun = A pura força do(a) { THE($cannon) } te derruba!

