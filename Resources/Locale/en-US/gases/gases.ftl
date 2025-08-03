gases-oxygen = Oxygen
gases-nitrogen = Nitrogen
gases-co2 = Carbon Dioxide
gases-plasma = Plasma
gases-tritium = Tritium
gases-water-vapor = Water Vapor
gases-ammonia = Ammonia
gases-n2o = Nitrous Oxide
gases-frezon = Frezon

atmospherics-gas-name = { $gas ->
    [Oxygen] { gases-oxygen }
    [Nitrogen] { gases-nitrogen }
    [CarbonDioxide] { gases-co2 }
    [Plasma] { gases-plasma }
    [Tritium] { gases-tritium }
    [WaterVapor] { gases-water-vapor }
    [Ammonia] { gases-ammonia }
    [NitrousOxide] { gases-n2o }
    [Frezon] { gases-frezon }
    *[else] Unknown gas
}
