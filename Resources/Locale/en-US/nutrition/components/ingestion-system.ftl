### Interaction Messages

# System

## When trying to ingest without the required utensil... but you gotta hold it
ingestion-you-need-to-hold-utensil = You need to be holding {INDEFINITE($utensil)} {$utensil} to eat that!

ingestion-try-use-is-empty = {CAPITALIZE(THE($entity))} is empty!
ingestion-try-use-wrong-utensil = You can't {$verb} {THE($food)} with {INDEFINITE($utensil)} {$utensil}.

ingestion-remove-mask = You need to take off the {$entity} first.

## Failed Ingestion

ingestion-you-cannot-ingest-any-more = You can't {$verb} any more!
ingestion-other-cannot-ingest-any-more = {CAPITALIZE(SUBJECT($target))} can't {$verb} any more!

ingestion-cant-digest = You can't digest {THE($entity)}!
ingestion-cant-digest-other = {CAPITALIZE(SUBJECT($target))} can't digest {THE($entity)}!

## Action Verbs, not to be confused with Verbs

ingestion-verb-food = Eat
ingestion-verb-drink = Drink

# Edible Component

edible-nom = Nom. {$flavors}
edible-nom-other = Nom.
edible-slurp = Slurp. {$flavors}
edible-slurp-other = Slurp.
edible-swallow = You swallow { THE($food) }
edible-gulp = Gulp. {$flavors}
edible-gulp-other = Gulp.

edible-has-used-storage = You cannot {$verb} { THE($food) } with an item stored inside.

## Nouns

edible-noun-edible = edible
edible-noun-food = food
edible-noun-drink = drink
edible-noun-pill = pill

## Verbs

edible-verb-edible = ingest
edible-verb-food = eat
edible-verb-drink = drink
edible-verb-pill = swallow

## Force feeding

edible-force-feed = {CAPITALIZE(THE($user))} is trying to make you {$verb} something!
edible-force-feed-success = {CAPITALIZE(THE($user))} forced you to {$verb} something! {$flavors}
edible-force-feed-success-user = You successfully feed {THE($target)}
