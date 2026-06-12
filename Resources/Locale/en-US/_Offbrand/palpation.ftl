palpation-nothing = You don't feel anything of note.
palpation-feels = You feel {$feels}.

-palpation-pulse-quality = { $quality ->
    [normal] normal
    [slightlyweak] slightly weakened
    [weak] weak
   *[weakest] very weak
}

palpation-pulse =
    .normal = a { -palpation-pulse-quality(quality: $quality) } pulse
    .fast = a fast, { -palpation-pulse-quality(quality: $quality) } pulse
    .veryfast = a rapid, { -palpation-pulse-quality(quality: $quality) } pulse
    .dangerous = a dangerously fast, { -palpation-pulse-quality(quality: $quality) } pulse


palpation-arterial-bleeding = warm blood welling against your fingers in time with the pulse
palpation-broken-bone = something inside moving with your touch
palpation-cut-tendon = that { THE($organ) } gives way to your touch without resistance
