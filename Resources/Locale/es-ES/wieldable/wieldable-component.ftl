### Locale for wielding items; i.e. two-handing them

wieldable-verb-text-wield = Empuñar
wieldable-verb-text-unwield = Desesgrimir

wieldable-component-successful-wield = Tu empuñas { THE($item) }.
wieldable-component-failed-wield = Tú desenvainas { EL($artículo) }.
wieldable-component-successful-wield-other = { CAPITALIZE(THE($user)) } blande { THE($item) }.
wieldable-component-failed-wield-other = { CAPITALIZE(THE($user)) } desenfunda { THE($item) }.

wieldable-component-no-hands = ¡No tienes suficientes manos!
wieldable-component-not-enough-free-hands = {$number ->
    [one] Necesitas una mano libre para blandir { THE($item) }.
    *[other] Necesitas { $number } manos libres para blandir { THE($item) }.
}
wieldable-component-not-in-hands = ¡{ CAPITALIZE(THE($item)) } no está en tus manos!

wieldable-component-requires = ¡{ CAPITALIZE(THE($item))} debe ser empuñado!

gunwieldbonus-component-examine = Esta arma tiene una precisión mejorada cuando se empuña.

gunrequireswield-component-examine = Esta arma sólo se puede disparar cuando se empuña.