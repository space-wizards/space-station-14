### Interaction Messages

# System

## When trying to ingest without the required utensil... but you gotta hold it

ingestion-you-need-to-hold-utensil = Вам нужна { $utensil }, чтобы есть это!
ingestion-try-use-is-empty = { CAPITALIZE($entity) } пуст!
ingestion-try-use-wrong-utensil = Вы не можете { $verb } { $food } с помощью { $utensil }.
ingestion-remove-mask = Сперва снимите { $entity }.

## Failed Ingestion

ingestion-you-cannot-ingest-any-more = Вы не можете больше { $verb }!
ingestion-other-cannot-ingest-any-more = { CAPITALIZE(SUBJECT($target)) } не может больше { $verb }!
ingestion-cant-digest = Вы не сможете переварить { $entity }!
ingestion-cant-digest-other = { CAPITALIZE(SUBJECT($target)) } не сможет переварить { $entity }!

## Action Verbs, not to be confused with Verbs

ingestion-verb-food = Есть
ingestion-verb-drink = Пить

# Edible Component

edible-nom = Ням. { $flavors }
edible-nom-other = Ням.
edible-slurp = Сёрб. { $flavors }
edible-slurp-other = Сёрб.
edible-swallow = Вы проглатываете { $food }
edible-gulp = Глоть. { $flavors }
edible-gulp-other = Глоть.
edible-has-used-storage = Вы не можете { $verb } { $food }, пока внутри что-то есть.

## Nouns

edible-noun-edible = съедобное
edible-noun-food = еда
edible-noun-drink = напиток
edible-noun-pill = таблетка

## Verbs

edible-verb-edible = поглощать
edible-verb-food = есть
edible-verb-drink = пить
edible-verb-pill = глотать

## Force feeding

edible-force-feed = { CAPITALIZE($user) } пытается заставить вас что-то { $verb }!
edible-force-feed-success = { CAPITALIZE($user) } заставил вас что-то { $verb }! { $flavors }
edible-force-feed-success-user = Вы успешно накормили { $target }
