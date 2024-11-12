import argparse
from dataclasses import dataclass
import mimetypes
import sys
from enum import Enum, auto
from pathlib import Path
from typing import Any, Sequence

import yaml

class ValidationFailure(Enum):
    ### Scan failures
    # Could not determine the file mimetype from its full name. Likely something got lost or misnamed
    UNKNOWN = auto()
    # YAML files that exist outside of the dedicated directories, and don't seem to be associated with any data
    # Most likely just files that have a typo in their name
    ORPHANED = auto()
    ### attributions.yml failures
    # A value within `files` array is invalid in some way
    INVALID_FILE_ENTRY = auto()
    # License is set as "Custom" but has no clarification whether or not the assets can be used commercially
    CUSTOM_LICENSE_UNCLARIFIED = auto()
    # License prohibits non-commercial use, but the licensing configuration or flags ask for checking that assets are okay to use
    NON_COMMERCIAL_LICENSE_VIOLATION = auto()
    # License was explicitly listed as invalid in the config
    LICENSE_INVALID = auto()
    # Entry uses a source that's not been vetted as valid. Most applicable to audio files
    SOURCE_UNKNOWN = auto()
    # The source is known to use specific licenses, but the attribution entry uses another
    SOURCE_LICENSE_MISMATCH = auto()
    # Attribution describes an asset that is missing. Most likely a typo, or reference to now deleted asset
    ASSET_MISSING = auto()
    # Asset has already been described by another entry. Most likely a failure to increment numbered files during copy-pasting
    ASSET_DUPLICATE_ATTRIBUTION = auto()
    ### Other validations
    # Asset exists but there's no attributions.yml file associated with it
    ATTRIBUTIONS_YML_MISSING = auto()
    ### The entire reason for this script to exist
    # Asset has no attribution on the record
    UNATTRIBUTED = auto()

def validation_enum(validations: str):
    try:
        separated = set([ValidationFailure[v] for v in validations.split(",")])
    except KeyError as ex:
        raise argparse.ArgumentTypeError(f"Invalid validation enum value - \"{ex.args[0]}\"")
    return separated

def existing_path(path: str):
    try:
        path = Path(path)
    except:
        raise argparse.ArgumentTypeError(f"Invalid path - \"{path}\"")
    if not path.exists():
        raise argparse.ArgumentTypeError(f"Path does not exist - \"{path}\"")
    return path

class ExtendSetAction(argparse.Action):
    def __call__(self, parser, namespace, values, option_string) -> None:
        previous: set[ValidationFailure] | None = getattr(namespace, self.dest)
        if previous is None:
            previous = set()
        if values is None:
            values = set(ValidationFailure)

        setattr(namespace, self.dest, previous.union(values))

class SingleMetavarHelpFormatter(argparse.HelpFormatter):
    def _format_action_invocation(self, action: argparse.Action) -> str:
        metavar = action.metavar or action.dest
        nargs_desc = ""
        flaglikes = len(action.option_strings)

        if action.nargs == "?" and flaglikes > 0:
            nargs_desc = f"({metavar})"
        elif action.nargs == "?" and flaglikes == 0:
            nargs_desc = f"{metavar}"
        elif action.nargs == "*":
            nargs_desc = f"{metavar}..."
        elif isinstance(action.nargs, int) or action.nargs:
            nargs_desc = f"{" ".join([metavar] * action.nargs)}"

        if len(action.option_strings) > 0:
            nargs_desc = " " + nargs_desc

        return (", ".join(action.option_strings) + nargs_desc).ljust(20)

ignore_mimetypes = [
    "application/json",
    "application/toml",
    "text/xml",
    "text/markdown",
    "text/plain",
    # just making shit up here
    "application/vnd.space-wizards.shader",
    "application/vnd.mozilla.fluent",
]

asset_mimetypes = [
    "image/png",
    "image/webp",
    "image/svg+xml",
    "image/vnd.microsoft.icon",
    "audio/ogg",
    "audio/x-sf2", # Made up again
    # (Some of) These have actual license files lying nearby, but... yeah
    "font/otf",
    "font/ttf",
]

yaml_paths = [
    # Dirs
    "Changelog",
    "Credits",
    "IgnoredPrototypes",
    "Maps",
    "Prototypes",
    # Files
    "clientCommandPerms.yml",
    "engineCommandPerms.yml",
    "keybinds.yml",
    "licensing.yml",
    "manifest.yml",
    "mapping_actions.yml",
    "migration.yml",
    "toolshedEngineCommandPerms.yml",
]

# List them in a way that more specific paths is before the more general one

source_validation = [
    ("https://freesound.org/", [
        "CC-BY-3.0",
        "CC-BY-4.0",
        "CC-BY-SA-3.0",
        "CC-BY-SA-4.0",
        "CC-BY-NC-3.0",
        "CC-BY-NC-4.0",
        "CC-BY-NC-SA-3.0",
        "CC-BY-NC-SA-4.0",
        "CC0-1.0",
        "Custom" # because sampling was not added to list of valid licenses elsewhere yay
    ]),
    ("https://github.com/space-wizards/space-station-14", "*"),
    ("https://github.com/goonstation/goonstation", "CC-BY-NC-SA-3.0")
]

# Should be a CLI flag
list_unattributed_details = True

class ValidationFailureDetails:
    affected_data: str = None
    relevant_filepath: object = None

    def __init__(self, affected_data, relevant_filepath):
        self.affected_data = affected_data
        self.relevant_filepath = relevant_filepath
        pass

    def __repr__(self):
        return f"ValidationFailureDetails({self.affected_data}, {self.relevant_filepath})"

    def __str__(self):
        return f"{self.relevant_filepath}: {self.affected_data}"

validation_failure_names = {
    ValidationFailure.UNKNOWN: "Unknown files",
    ValidationFailure.ORPHANED: "Orphaned YAML files",
    ValidationFailure.INVALID_FILE_ENTRY: "Invalid file entries",
    ValidationFailure.CUSTOM_LICENSE_UNCLARIFIED: "Assets with custom license without commercial use clarification flag",
    ValidationFailure.NON_COMMERCIAL_LICENSE_VIOLATION: "VIOLATION OF A NON-COMMERCIAL LICENSE",
    ValidationFailure.LICENSE_INVALID: "License configured as invalid",
    ValidationFailure.SOURCE_UNKNOWN: "Source is not known to be valid",
    ValidationFailure.SOURCE_LICENSE_MISMATCH: "Source license usage doesn't match",
    ValidationFailure.ASSET_MISSING: "Missing (or misspelt) assets",
    ValidationFailure.ASSET_DUPLICATE_ATTRIBUTION: "Assets described by different attribution entries",
    ValidationFailure.ATTRIBUTIONS_YML_MISSING: "Missing (or misspelt) attributions.yml files",
    ValidationFailure.UNATTRIBUTED: "Unattributed assets",
}

mimetypes.add_type("application/yaml", ".yaml")
mimetypes.add_type("application/toml", ".toml")
mimetypes.add_type("application/vnd.mozilla.fluent", ".ftl", False)
mimetypes.add_type("application/vnd.space-wizards.shader", ".swsl", False)
mimetypes.add_type("audio/x-sf2", ".sf2", False)

mimetypes.suffix_map[".yml"] = ".yaml"

class LicensingConfig:
    # Assume the worst and most strict case for this stuff always
    non_commercial = False
    ignored_validation: set[ValidationFailure]
    invalid_licenses: list[str]
    list_unattributed_stats: bool = False

    def __init__(self, config_data: dict[Any]):
        if not isinstance(config_data, dict):
            raise ValueError("Configuration file \"licensing.yml\" is invalid, check the syntax!")
        self.non_commercial = config_data.get("project_is_non_commercial_and_not_for_profit", False)
        ignoring = config_data.get("ignore_checks", list())
        self.ignored_validation = set([ValidationFailure[v] for v in ignoring])
        self.invalid_licenses = config_data.get("invalid_licenses", [])


class LicensingScan:
    """Storage class for all files relevant to licensing that were found in a directory scan"""
    # Asset files
    assets: list[Path] = []
    # Attribution files, to be processed for matching with asset files
    attributions: list[Path] = []
    # YAML files outside of known path, not an attributions file and does not have associated image file
    orphaned: list[Path] = []
    # Shrug
    unknown: list[Path] = []

    path: Path = None

    def __init__(self, path: str | Path, ):
        if isinstance(path, str):
            path = Path(path)
        self.path = path

    def run(self):
        for path in self.path.rglob("*"):
            if path.is_dir():
                continue

            if path.name == "attributions.yml":
                self.attributions.append(path)
                continue

            (ext_mime, _) = mimetypes.guess_type(path, False)

            if ext_mime is None and path.name.lower() == "readme":
                ext_mime = "text/plain"

            if ext_mime is None:
                self.unknown.append(path)
                continue

            if ext_mime in ignore_mimetypes:
                continue

            if ext_mime == "application/yaml":
                if path.parts[1] in yaml_paths:
                    continue
                # It's a semi-automatically generated file, or metadata for something
                # TODO: make a better system for these
                if path.with_suffix("").exists():
                    continue

                self.orphaned.append(path)
                continue

            # Similar, and very related, to above yaml thing
            if path.name.endswith("dpi.png") and path.with_suffix("").with_suffix("").exists():
                continue

            if ext_mime not in asset_mimetypes:
                print("UNKNOWN OR INVALID MIMETYPE: ", path, ext_mime)

            # Ignore things that are gitignored
            if path.parts[1] == "MapImages":
                continue

            # This is to be checked by a dedicated RSI validation script
            part_of_rsi = False
            for part in path.parts:
                if (part.endswith(".rsi")):
                    part_of_rsi = True
            if part_of_rsi:
                continue

            self.assets.append(path)

class LicensingValidation:
    """A class for drawing conclusions about a directory scan"""

    scan: LicensingScan
    config: LicensingConfig

    # Dictionary of the attribution objects for a given file
    attribution_dict: dict[str, object] = {}
    # List of assets that have no associated attribution file
    unattributed_assets = []
    # Per-folder count of total assets found and unattributed assets
    asset_folders_count: dict[str, int] = {}
    unattributed_folders_count: dict[str, int] = {}

    failures: dict[ValidationFailure, list[str | Path | ValidationFailureDetails]] = {}

    def __init__(self, scan: LicensingScan):
        self.scan = scan

        if (scan.path / "licensing.yml").exists():
            with (scan.path / "licensing.yml").open() as handle:
                licensing_config_data = yaml.safe_load(handle)
                self.config = LicensingConfig(licensing_config_data)
        else:
            self.config = LicensingConfig({})

    def record_failure(self, failure_type: ValidationFailure, files: list[str] | str, relevant_filepath: str = None):
        if not isinstance(files, list):
            files = [ files ]

        if relevant_filepath is not None:
            detailed_files = []
            for item in files:
                if not isinstance(files, ValidationFailureDetails):
                    detailed_files.append(ValidationFailureDetails(item, relevant_filepath))
                else:
                    detailed_files.append(item)
            files = detailed_files

        if isinstance(files, list):
            self.failures.setdefault(failure_type, []).extend(files)
        else:
            self.failures.setdefault(failure_type, []).append(files)

    def process_attribution_data(self, filepath, data):
        for attribution in data:
            files = attribution["files"]
            if len(files) == 0:
                print(f"ATTRIBUTION ENTRY WITH NO FILES IN \"{filepath}\"")
                self.record_failure(ValidationFailure.INVALID_FILE_ENTRY, filepath)

            if "Custom" == attribution["license"]:
                if "commercialUseAllowed" not in attribution:
                    self.record_failure(ValidationFailure.CUSTOM_LICENSE_UNCLARIFIED, files, filepath)

            non_commercial_asset_misuse = False
            if not self.config.non_commercial:
                if "-NC-" in attribution["license"]:
                    non_commercial_asset_misuse = True
                if "Custom" == attribution["license"]:
                    # Let's err on the safe side here
                    non_commercial_asset_misuse = ("commercialUseAllowed" not in attribution) or (attribution["commercialUseAllowed"] == False)

            license_invalid = False
            if attribution["license"] in self.config.invalid_licenses:
                license_invalid = True

            source_known_valid = False
            for source in source_validation:
                if source[0] in attribution["source"]:
                    source_known_valid = True
                    license_mismatch = True
                    if isinstance(source[1], list):
                        for license in source[1]:
                            if license == attribution["license"]:
                                license_mismatch = False
                    elif source[1] == attribution["license"]:
                        license_mismatch = False
                    if source[1] == "*":
                        license_mismatch = False
                    if license_mismatch:
                        self.record_failure(ValidationFailure.SOURCE_LICENSE_MISMATCH, attribution["source"], filepath)
                        print(f"SOURCE LICENSE MISMATCH, PLEASE DOUBLE CHECK \"{filepath}\": \"{attribution["source"]}\" - \"{attribution["license"]}\", expected \"{source[1]}\"")

            if not source_known_valid:
                self.record_failure(ValidationFailure.SOURCE_UNKNOWN, attribution["source"], filepath)

            for file in files:
                try:
                    attributed_asset = filepath.with_name(file)
                    if non_commercial_asset_misuse:
                        self.record_failure(ValidationFailure.NON_COMMERCIAL_LICENSE_VIOLATION, file, filepath)
                    if license_invalid:
                        self.record_failure(ValidationFailure.LICENSE_INVALID, file, filepath)

                    if attributed_asset.exists():
                        if attributed_asset in self.attribution_dict:
                            self.record_failure(ValidationFailure.ASSET_DUPLICATE_ATTRIBUTION, file, filepath)

                        self.attribution_dict[attributed_asset] = data
                    else:
                        self.record_failure(ValidationFailure.ASSET_MISSING, file, filepath)
                except ValueError:
                    self.record_failure(ValidationFailure.INVALID_FILE_ENTRY, file)
                    if "/" in file or "\\" in file:
                        print(f"Tried to attribute a file within a subdirectory path in \"{filepath}\": \"{file}\"")
                    else:
                        print(f"Invalid file entry in \"{filepath}\": \"{file}\"")
                except OSError:
                    self.record_failure(ValidationFailure.INVALID_FILE_ENTRY, file)
                    print(f"Invalid file entry in \"{filepath}\": \"{file}\"")

    def check(self) -> bool:
        if len(self.scan.orphaned) > 0:
            self.record_failure(ValidationFailure.ORPHANED, self.scan.orphaned)
        if len(self.scan.unknown) > 0:
            self.record_failure(ValidationFailure.UNKNOWN, self.scan.unknown)

        # Process attribution files, and all files mentioned in them
        for attribution_file in self.scan.attributions:
            with attribution_file.open() as handle:
                data = yaml.safe_load(handle)
                self.process_attribution_data(attribution_file, data)

        for asset in self.scan.assets:
            parent_path = asset.parent
            self.asset_folders_count[parent_path] = self.asset_folders_count.get(parent_path, 0) + 1
            if asset in self.attribution_dict:
                continue
            self.unattributed_folders_count[parent_path] = self.unattributed_folders_count.get(parent_path, 0) + 1
            self.unattributed_assets.append(asset)

        if len(self.unattributed_assets) > 0:
            self.record_failure(ValidationFailure.UNATTRIBUTED, self.unattributed_assets)

            printed_header = False
            for path, unatt_count in self.unattributed_folders_count.items():
                total_count = self.asset_folders_count[path]

                have_attributions_yml = ""
                if not (Path(path) / "attributions.yml").exists():
                    have_attributions_yml = " - no attributions.yml, shown later as !!!" if not printed_header else " !!!"
                    self.record_failure(ValidationFailure.ATTRIBUTIONS_YML_MISSING, path)

                if not self.config.list_unattributed_stats:
                    continue

                if not printed_header:
                    print("Missing attributions per folder:")
                    printed_header = True
                print(f"- {path}: {unatt_count}/{total_count} ({float(unatt_count) / float(total_count) * 100:.1f}%){have_attributions_yml}")

            if not self.config.list_unattributed_stats:
                print()

        return len(self.failures) == 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="A tool for checking license compliance of a project that uses Robust General Attribution files",
        formatter_class=SingleMetavarHelpFormatter,
        add_help=False
    )
    parser.add_argument("--help", "-h", action="help", help="Print this help message and exit")
    parser.add_argument("--ignore-flags", "-i", type=validation_enum, action=ExtendSetAction, nargs="?", help=f"Validation failures to ignore for the exit status, comma separated (Default: Empty): {", ".join([e._name_ for e in ValidationFailure])}")
    parser.add_argument("--hide-details", "-d", type=validation_enum, action=ExtendSetAction, nargs="?", help="Hide details for specified validations, comma separated or empty for all")
    parser.add_argument("--unattributed-stats", action="store_true", help="Treat non-commercial licenses as invalid", default=None)
    parser.add_argument("--commercial", action="store_true", help="Treat non-commercial licenses as invalid", default=False)
    parser.add_argument("--ignore-licensing-config", action="store_true", help="Ignore licensing.yml in the specified Resources directory", default=False)
    parser.add_argument("--resources", type=existing_path, help="Resource directory to scan (Default: ./Resources/)", default=Path("Resources"))
    parser.add_argument("filelist", type=existing_path, nargs="?", help="Path to a file containing a list of files to check. Everything else is ignored", default=None)

    args = parser.parse_args()

    scan = LicensingScan(args.resources)
    scan.run()
    validation = LicensingValidation(scan)
    if args.ignore_flags is not None:
        validation.config.ignored_validation = args.ignore_flags
    if args.unattributed_stats is not None:
        validation.config.list_unattributed_stats = args.unattributed_stats

    validation.check()

    print(f"Found {len(validation.scan.assets)} assets, described by {len(validation.scan.attributions)} attributions.yml files")
    print()

    if len(validation.failures) > 0:
        fail_exit = False
        print("Assets attribution validation failed!")
        if len(validation.config.ignored_validation) > 0:
            print(f"Some checks have been ignored: {", ".join([e._name_ for e in validation.config.ignored_validation])}")
        print()

        for failure_type in ValidationFailure:
            if failure_type not in validation.failures:
                continue
            ignored = ""
            if not failure_type in validation.config.ignored_validation:
                fail_exit = True
            else:
                ignored = " - IGNORED!"
            if args.hide_details is not None and failure_type in args.hide_details:
                continue
            failures = validation.failures[failure_type]

            print(f"{validation_failure_names.get(failure_type, str(failure_type))} ({len(failures)}){ignored}:")
            for entry in failures:
                print(f"- {entry}")
            print()

        print("Summary:")
        for failure_type in ValidationFailure:
            if failure_type not in validation.failures:
                continue
            failures = validation.failures[failure_type]
            ignored = "" if failure_type not in validation.config.ignored_validation else " - IGNORED!"
            print(f"- {validation_failure_names.get(failure_type, str(failure_type))}: {len(failures)}{ignored}")

        if fail_exit:
            sys.exit(1)
    else:
        print("Assets attribution validation ok!")