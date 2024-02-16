ent-AlwaysPoweredWallLight = light
    .desc = []
    .suffix = Always powered

ent-PoweredlightEmpty = light
    .desc = []
    .suffix = Empty

ent-Poweredlight = { ent-PoweredlightEmpty }
    .desc = []
    .suffix = ""

ent-PoweredlightLED = { ent-Poweredlight }
    .desc = []
    .suffix = LED

ent-AlwaysPoweredLightLED = { ent-AlwaysPoweredWallLight }
    .desc = { ent-AlwaysPoweredWallLight.desc }
    .suffix = Always Powered, LED

ent-PoweredlightExterior = { ent-Poweredlight }
    .desc = []
    .suffix = Blue

ent-AlwaysPoweredLightExterior = { ent-AlwaysPoweredWallLight }
    .desc = { ent-AlwaysPoweredWallLight.desc }
    .suffix = Always Powered, Blue

ent-PoweredlightSodium = { ent-Poweredlight }
    .desc = []
    .suffix = Sodium

ent-AlwaysPoweredLightSodium = { ent-AlwaysPoweredWallLight }
    .desc = { ent-AlwaysPoweredWallLight.desc }
    .suffix = Always Powered, Sodium

ent-SmallLight = small light
    .desc = []
    .suffix = Always Powered

ent-PoweredSmallLightEmpty = small light
    .desc = []
    .suffix = Empty

ent-PoweredSmallLight = { ent-PoweredSmallLightEmpty }
    .desc = { ent-PoweredSmallLightEmpty.desc }
    .suffix = ""

ent-EmergencyLight = emergency light
    .desc = A small light with an internal battery that turns on as soon as it stops receiving any power. Nanotrasen technology allows it to adapt its color to alert crew to the conditions of the station.
    .suffix = ""

