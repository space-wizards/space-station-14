book-text-atmos-distro = The distribution network, or "distro" for short, is the station's lifeline. It's responsible for transporting air from atmospherics throughout the station.

        Relevant pipes are often painted Popping Subdued Blue, but a surefire way to identify them is to use a tray scanner to trace which pipes are connected to active vents on the station.

        The standard gas mix of the distribution network is 20 degrees celsius, 78% nitrogen, 22% oxygen. You can check this by using a gas analyzer on a distro pipe or any vent connected to it. Special circumstances may call for special mixes.

        When it comes to deciding on a distro pressure, there are a few things to consider. Active vents will regulate the station's pressure, so as long as everything is functioning properly, there's no such thing as too high of a distro pressure.

        A higher distro pressure will allow the distro network to act as a buffer between the gas miners and vents, providing a significant amount of extra air that can be used to re-pressurize the station after a spacing.

        A lower distro pressure will reduce the amount of gas lost in the event that the distro is spaced, a quick way to deal with distro contamination. It can also help slow or prevent over-pressurization of the station in the event of vent issues.

        Common distro pressures are in the range of 300-375 kPa, but other pressures can be used with knowledge of the risks and benefits.

        The pressure of the network is determined by the last pump pumping into it. To prevent bottlenecks, all other pumps between the miners and the last pump should be set to their maximum rate, and any unnecessary devices should be removed.

        You can validate the distro pressure with a gas analyzer, but keep in mind that high demand due to things like spacings can cause the distro to be below the set target pressure for extended periods. So, if you see a dip in pressure, don't panic - it might be temporary.

book-text-atmos-waste = The waste network is the primary system responsible for keeping the air on the station free of contaminants.

        You can identify the relevant pipes by their Pleasing Dull Red color or by using a tray scanner to trace which pipes are connected to the scrubbers on the station.

        The waste network is used to transport waste gasses to either be filtered or spaced. It is ideal to keep the pressure at 0 kPa, but it may sometimes be at a low non-zero pressure while in use.

        Technicians have the option to filter or space the waste gasses. While spacing is faster, filtering allows for the gasses to be reused for recycling or selling.

        The waste network can also be used to diagnose atmospheric issues on the station. High levels of a waste gas may suggest a large leak, while the presence of non-waste gases may indicate a scrubber configuration or physical connection issue. If the gases are at a high temperature, it could indicate a fire.

book-text-atmos-alarms = Air alarms are located throughout stations to allow management and monitoring of the local atmosphere.

            The air alarm interface provides technicians with a list of connected sensors, their readings, and the ability to adjust thresholds. These thresholds are used to determine the alarm condition of the air alarm. Technicians can also use the interface to set target pressures for vents and configure the operating speeds and targeted gases for scrubbers.

            While the interface allows for fine-tuning of the devices under the air alarm's control, there are also several modes available for rapid configuration of the alarm. These modes are automatically switched to when the alarm state changes:
            - Filtering: The default mode
            - Filtering (wide): A filtering mode that modifies the operation of scrubbers to scrub a wider area
            - Fill: Disables scrubbers and sets vents to their maximum pressure
            - Panic: Disables vents and sets scrubbers to siphon

            A multitool or network configurator can be used to link devices to air alarms.

book-text-atmos-vents =
    Below is a quick reference guide to several atmospheric devices:

                Passive Vents:
                These vents don't require power, they allow gases to flow freely both into and out of the pipe network they are attached to.

                Active Vents:
                These are the most common vents on the station. They have an internal pump, and require power. By default, they will only pump gases out of pipes, and only up to 101 kpa. However, they can be reconfigured using an air alarm. They will also lock out if the room is under 1 kpa, to prevent pumping gasses into space.

                Air Scrubbers:
                These devices allow gases to be removed from the environment and put into the connected pipe network. They can be configured to select specific gases when connected to an air alarm.

                Air Injectors:
                Injectors are similar to active vents, but they have no internal pump and do not require power. They cannot be configured, but they can continue to pump gasses up to much higher pressures.
