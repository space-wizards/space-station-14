construction-presenter-to-node-to-node = To perform this, first you need to:

construction-examine-status-effect-should-have = The target needs to have { $effect }.
construction-examine-status-effect-should-not-have = The target needs to not have { $effect }.
construction-step-condition-status-effect-should-have = The target needs to have { $effect }.
construction-step-condition-status-effect-should-not-have = The target needs to not have { $effect }.

construction-examine-heart-damage-range = { $max ->
    [2147483648] The target needs to have at least {NATURALFIXED($min, 2)} heart damage.
    *[other] { $min ->
                [0] The target needs to have at most {NATURALFIXED($max, 2)} heart damage.
                *[other] The target needs to have between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} heart damage.
             }
}

construction-step-heart-damage-range = { $max ->
    [2147483648] The target needs to have at least {NATURALFIXED($min, 2)} heart damage.
    *[other] { $min ->
                [0] The target needs to have at most {NATURALFIXED($max, 2)} heart damage.
                *[other] The target needs to have between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} heart damage.
             }
}

construction-examine-lung-damage-range = { $max ->
    [2147483648] The target needs to have at least {NATURALFIXED($min, 2)} lung damage.
    *[other] { $min ->
                [0] The target needs to have at most {NATURALFIXED($max, 2)} lung damage.
                *[other] The target needs to have between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} lung damage.
             }
}

construction-step-lung-damage-range = { $max ->
    [2147483648] The target needs to have at least {NATURALFIXED($min, 2)} lung damage.
    *[other] { $min ->
                [0] The target needs to have at most {NATURALFIXED($max, 2)} lung damage.
                *[other] The target needs to have between {NATURALFIXED($min, 2)} and {NATURALFIXED($max, 2)} lung damage.
             }
}

construction-component-to-perform-header = To perform {$targetName}...
