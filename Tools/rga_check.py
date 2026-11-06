import argparse
import mimetypes
import sys
import urllib.parse as urlparse
from enum import Enum, auto
from pathlib import Path
from typing import Any

import yaml

class ValidationFailure(Enum):
    ### Scan failures
    # Could not determine the file mimetype from its full name. Likely something got lost or misnamed
    UNKNOWN = auto()
    # YAML files that exist outside of the dedicated directories, and don't seem to be associated with any data
    # Most likely just files that have a typo in their name
    FILE_ORPHANED_YAML = auto()
    ### attributions.yml failures
    # A value within `files` array is invalid in some way
    FILE_ENTRY_INVALID = auto()
    # License is set as "Custom" but has no clarification whether or not the assets can be used commercially
    LICENSE_CUSTOM_UNCLARIFIED = auto()
    # TODO: License is "custom" and the custom license file is missing
    LICENSE_CUSTOM_MISSING = auto()
    # License prohibits non-commercial use, but the licensing configuration or flags ask for checking that assets are okay to use
    LICENSE_VIOLATION_NON_COMMERCIAL = auto()
    # License was explicitly listed as invalid in the config
    LICENSE_REJECTED = auto()
    # Entry uses a source that's not been vetted as valid. Most applicable to audio files
    SOURCE_UNKNOWN = auto()
    # The source is known to use specific licenses, but the attribution entry uses another
    SOURCE_LICENSE_MISMATCH = auto()
    # The source is known and is configured to reject
    SOURCE_REJECTED = auto()
    # Attribution describes an asset that is missing. Most likely a typo, or reference to now deleted asset
    ASSET_MISSING = auto()
    # Asset has already been described by another entry. Most likely a failure to increment numbered files during copy-pasting
    ASSET_DUPLICATE_ATTRIBUTION = auto()
    ### Other validations
    # Asset exists but there's no attributions.yml file associated with it
    FILE_ATTRIBUTIONS_MISSING = auto()
    ### The entire reason for this script to exist
    # Asset has no attribution on the record
    UNATTRIBUTED = auto()

def string_to_enum(enum_class: Enum, value: str):
    try:
        separated = set([enum_class[v] for v in value.split(",") if v != ""])
    except KeyError as ex:
        raise argparse.ArgumentTypeError(f"Invalid {enum_class.__name__} enum value - \"{ex.args[0]}\"")
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
        return f"ValidationFailureDetails(\"{self.affected_data}\", {self.relevant_filepath})"

    def __str__(self):
        if " " in self.affected_data:
            return f"{self.relevant_filepath}: \"{self.affected_data}\""
        return f"{self.relevant_filepath}: {self.affected_data}"

validation_failure_names = {
    ValidationFailure.UNKNOWN: "Unknown files",
    ValidationFailure.FILE_ORPHANED_YAML: "Orphaned YAML files",
    ValidationFailure.FILE_ENTRY_INVALID: "Invalid file entries",
    ValidationFailure.LICENSE_CUSTOM_UNCLARIFIED: "Assets with custom license without commercial use clarification flag",
    ValidationFailure.LICENSE_CUSTOM_MISSING: "Assets with custom license missing the license file",
    ValidationFailure.LICENSE_VIOLATION_NON_COMMERCIAL: "VIOLATION OF A NON-COMMERCIAL LICENSE",
    ValidationFailure.LICENSE_REJECTED: "License rejected as per configuration",
    ValidationFailure.SOURCE_UNKNOWN: "Source is not known to be valid",
    ValidationFailure.SOURCE_LICENSE_MISMATCH: "Source license usage doesn't match",
    ValidationFailure.SOURCE_REJECTED: "Source rejected as per configuration",
    ValidationFailure.ASSET_MISSING: "Missing (or misspelt) assets",
    ValidationFailure.ASSET_DUPLICATE_ATTRIBUTION: "Assets described by multiple attribution entries",
    ValidationFailure.FILE_ATTRIBUTIONS_MISSING: "Missing (or misspelt, unmodified) attributions.yml files",
    ValidationFailure.UNATTRIBUTED: "Unattributed assets",
}

class SourceStatus(Enum):
    NOT_FOUND = auto()
    # Aka "skip"
    NOT_APPLICABLE = auto()
    REJECTED = auto()
    ACCEPTED = auto()
    LICENSE_MISMATCH = auto()
    # When the source is valid but the attribution format is wrong (not specific enough, etc)
    MISFORMATTED = auto()

class LicensingSource:
    """Class for matching sources against and figuring out """

    urls: list[urlparse.ParseResult]
    licenses: list[str]
    licenses_wildcard: bool = False
    reject_source: bool = False

    def __init__(self, yaml_data: dict[Any]):
        if not isinstance(yaml_data, dict):
            raise ValueError("Licensing source is not a dict")
        yaml_urls: list[str] = yaml_data.get("urls", list())
        yaml_urls.append(yaml_data.get("url", None))
        if len(yaml_urls) == 0:
            raise ValueError("Licensing source has no URLs defined")
        self.urls = [ urlparse.urlparse(s) for s in yaml_urls ]
        yaml_licenses: list[str] = [ x.strip() for x in yaml_data.get("licenses", list()) ]
        if "*" in yaml_licenses:
            self.licenses_wildcard = True
        self.licenses = [ x for x in yaml_licenses if x != "*" ]
        self.reject_source = yaml_data.get("reject", False)

    def source_match(self, source: str) -> bool:
        source_url = urlparse.urlparse(source)
        for url in self.urls:
            if source_url.netloc != url.netloc:
                continue
            if not source_url.path.startswith(url.path):
                continue
            return True
        return False

    def validate(self, attribution: dict[str]) -> SourceStatus:
        if not self.source_match(attribution["source"]):
            return SourceStatus.NOT_APPLICABLE
        if self.reject_source:
            return SourceStatus.REJECTED
        if self.licenses_wildcard:
            return SourceStatus.ACCEPTED
        if attribution["license"] not in self.licenses:
            return SourceStatus.LICENSE_MISMATCH
        return SourceStatus.ACCEPTED

class LicensingConfig:
    # Assume the worst and most strict case for this stuff always
    non_commercial: bool = False
    ignored_validation: set[ValidationFailure]
    reject_licenses: list[str]

    sources: list[LicensingSource] = []

    mimetypes_ignore: list[str]
    mimetypes_asset: list[str]
    yaml_paths: list[str]

    list_unattributed_stats: bool = False

    def __init__(self, source: str  | Path | dict[str, Any]):
        if isinstance(source, str):
            source = Path(source)
        if isinstance(source, Path):
            if source.is_dir:
                source = source / "licensing.yml"
            if not source.exists:
                raise ValueError(f"Configuration file does not exist at path \"{source}\"")
            with source.open() as handle:
                source = yaml.safe_load(handle)
        if not isinstance(source, dict):
            raise ValueError("Configuration file is invalid, check the syntax!")

        self.non_commercial = source.get("projectIsNonCommercial", False)
        ignoring = source.get("checksIgnored", list())
        self.ignored_validation = set([ValidationFailure[v] for v in ignoring])
        self.reject_licenses = source.get("licensesReject", [])

        self.mimetypes_ignore = source.get("mimetypesIgnore", [])
        self.mimetypes_asset = source.get("mimetypesAsset", [])
        self.yaml_paths = source.get("yamlPaths", [])

        for entry in source.get("mimetypes", {}):
            extensions: list[str] = entry["extensions"]
            first = extensions.pop(0)
            mimetypes.add_type(entry["type"], first, entry.get("iana", False))
            for ext in extensions:
                mimetypes.suffix_map[ext] = first

        for entry in source.get("sources", []):
            self.sources.append(LicensingSource(entry))

class LicensingScan:
    """Storage class for all files relevant to licensing that were found in a directory scan"""

    config: LicensingConfig
    filtered = False

    # Asset files
    assets: list[Path] = []
    # Attribution files, to be processed for matching with asset files
    attributions: list[Path] = []
    # YAML files outside of known path, not an attributions file and does not have associated image file
    orphaned: list[Path] = []
    # Shrug
    unknown: list[Path] = []

    path: Path = None

    def __init__(self, path: str | Path, config: LicensingConfig):
        if isinstance(path, str):
            path = Path(path)
        self.path = path
        self.config = config

    def filter(self, filter_list: set[str]):
        self.filtered = True

        # Make sure we don't annoy people when they're adding files, but do annoy if they're working on attributions stuff
        # Though, someone working on replacing files... well, they could run this manually? Hm.
        if all(f.endswith("/attributions.yml") for f in filter_list):
            attribution_files = [str(Path(x).parent) for x in filter_list if x.endswith("/attributions.yml")]

            # This isn't great but it's hard to do a set intersect between entirely different types?
            self.assets = [x for x in self.assets if str(x) in filter_list or str(x.parent) in attribution_files]
        else:
            self.assets = [x for x in self.assets if str(x) in filter_list]

        self.attributions = [x for x in self.attributions if str(x) in filter_list]
        return (len(self.assets), len(self.attributions))

    def run(self):
        for path in self.path.rglob("*"):
            if path.is_dir():
                continue

            if path.name == "attributions.yml":
                self.attributions.append(path)
                continue

            (ext_mime, _) = mimetypes.guess_type(path, False)

            # RSI image files can have path like "blahblah.rsi/.png"
            # Shrug
            if path.name.startswith("."):
                (ext_mime, _) = mimetypes.guess_type("empty" + path.name, False)

            if ext_mime is None and path.name.lower() == "readme":
                ext_mime = "text/plain"

            if ext_mime is None:
                self.unknown.append(path)
                continue

            if ext_mime in self.config.mimetypes_ignore:
                continue

            if ext_mime == "application/yaml":
                if path.parts[1] in config.yaml_paths:
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

            if ext_mime not in self.config.mimetypes_asset:
                print("UNKNOWN OR INVALID MIMETYPE: ", path, ext_mime)

            # Ignore things that are gitignored, yeah it's a hack
            # Feel free to improve. BUT! licensing.yml should not have an easy "ignore path" entry
            # That's too prone to abuse
            if path.parts[1] == "MapImages":
                continue

            # TODO: Checking RSI attribution (RGA too?)
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

    def __init__(self, scan: LicensingScan, config: LicensingConfig):
        self.scan = scan
        self.config = config

    def record_failure(self, failure_type: ValidationFailure, files: list[str | ValidationFailureDetails] | str, relevant_filepath: str = None):
        if not isinstance(files, list):
            files = [ files ]

        if relevant_filepath is not None:
            detailed_files: list[ValidationFailureDetails] = []
            for item in files:
                if isinstance(files, ValidationFailureDetails):
                    detailed_files.append(item)
                else:
                    detailed_files.append(ValidationFailureDetails(item, relevant_filepath))
            files = detailed_files

        if isinstance(files, list):
            self.failures.setdefault(failure_type, []).extend(files)
        else:
            self.failures.setdefault(failure_type, []).append(files)

    def process_attribution_data(self, filepath: Path, attribution):
        files = attribution["files"]
        if len(files) == 0:
            self.record_failure(ValidationFailure.FILE_ENTRY_INVALID, "NO FILES IN THE ATTRIBUTION ENTRY", filepath)

        non_commercial_asset_misuse = False
        if not self.config.non_commercial:
            if "-NC-" in attribution["license"]:
                non_commercial_asset_misuse = True
            if "Custom" == attribution["license"]:
                # Let's err on the safe side here
                non_commercial_asset_misuse = ("commercialUseAllowed" not in attribution) or (attribution["commercialUseAllowed"] == False)

        license_rejected = False
        if attribution["license"] in self.config.reject_licenses:
            license_rejected = True

        scan_includes = 0

        for file in files:
            try:
                attributed_asset = filepath.with_name(file)

                if not attributed_asset.exists():
                    self.record_failure(ValidationFailure.ASSET_MISSING, file, filepath)
                    continue
                if attributed_asset in self.attribution_dict:
                    self.record_failure(ValidationFailure.ASSET_DUPLICATE_ATTRIBUTION, file, filepath)
                if attributed_asset in self.scan.assets:
                    scan_includes += 1
                elif scan.filtered:
                    continue
                if non_commercial_asset_misuse:
                    self.record_failure(ValidationFailure.LICENSE_VIOLATION_NON_COMMERCIAL, file, filepath)
                if license_rejected:
                    self.record_failure(ValidationFailure.LICENSE_REJECTED, file, filepath)
                if "Custom" == attribution["license"] and "commercialUseAllowed" not in attribution:
                    self.record_failure(ValidationFailure.LICENSE_CUSTOM_UNCLARIFIED, file, filepath)

                self.attribution_dict[attributed_asset] = attribution
            except ValueError:
                self.record_failure(ValidationFailure.FILE_ENTRY_INVALID, file, filepath)
            except OSError:
                self.record_failure(ValidationFailure.FILE_ENTRY_INVALID, file, filepath)

        if scan.filtered and scan_includes == 0:
            return

        source_status = SourceStatus.NOT_FOUND
        for source in self.config.sources:
            status = source.validate(attribution)
            match status:
                case SourceStatus.NOT_APPLICABLE:
                    continue
                case SourceStatus.REJECTED:
                    source_status = status
                case SourceStatus.ACCEPTED:
                    source_status = status
                case SourceStatus.LICENSE_MISMATCH:
                    source_status = status
                case _:
                    raise Exception(f"Unknown source validation value {status}")

        if source_status == SourceStatus.NOT_FOUND:
            self.record_failure(ValidationFailure.SOURCE_UNKNOWN, attribution["source"], filepath)
        if source_status == SourceStatus.LICENSE_MISMATCH:
            self.record_failure(ValidationFailure.SOURCE_LICENSE_MISMATCH, attribution["source"], filepath)
        if source_status == SourceStatus.REJECTED:
            self.record_failure(ValidationFailure.SOURCE_REJECTED, attribution["source"], filepath)

    def check(self) -> bool:
        if len(self.scan.orphaned) > 0:
            self.record_failure(ValidationFailure.FILE_ORPHANED_YAML, self.scan.orphaned)
        if len(self.scan.unknown) > 0:
            self.record_failure(ValidationFailure.UNKNOWN, self.scan.unknown)

        # Process attribution files, and all files mentioned in them
        for attribution_file in self.scan.attributions:
            with attribution_file.open() as handle:
                data = yaml.safe_load(handle)
                for attribution in data:
                    self.process_attribution_data(attribution_file, attribution)

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
                    self.record_failure(ValidationFailure.FILE_ATTRIBUTIONS_MISSING, path / "attributions.yml")

                if not self.config.list_unattributed_stats:
                    continue

                if not printed_header:
                    print("Missing attributions per folder:")
                    printed_header = True
                print(f"- {path}: {unatt_count}/{total_count} ({float(unatt_count) / float(total_count) * 100:.1f}%){have_attributions_yml}")

        return len(self.failures) == 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="A tool for checking license compliance of a project that uses Robust General Attribution files",
        formatter_class=SingleMetavarHelpFormatter,
        add_help=False
    )
    parser.add_argument("--help", "-h", action="help", help="Print this help message and exit")
    parser.add_argument("--ignore-flags", "-i", type=lambda value: string_to_enum(ValidationFailure, value), action=ExtendSetAction, nargs="?", help=f"Validation failures to ignore for the exit status, comma separated (Default: Empty): {", ".join([e._name_ for e in ValidationFailure])}")
    parser.add_argument("--hide-details", "-d", type=lambda value: string_to_enum(ValidationFailure, value), action=ExtendSetAction, nargs="?", help="Hide details for specified validations, comma separated or empty for all")
    parser.add_argument("--show-details", "-D", type=lambda value: string_to_enum(ValidationFailure, value), action=ExtendSetAction, nargs="?", help="Only show details for specified validations, same format as others")
    parser.add_argument("--unattributed-stats", action="store_true", help="Treat non-commercial licenses as invalid", default=None)
    parser.add_argument("--commercial", action="store_true", help="Treat non-commercial licenses as invalid", default=False)
    parser.add_argument("--ignore-licensing-config", action="store_true", help="Ignore licensing.yml in the specified Resources directory", default=False)
    parser.add_argument("--resources", type=existing_path, help="Resource directory to scan (Default: ./Resources/)", default=Path("Resources"))
    parser.add_argument("filelist", type=existing_path, nargs="?", help="Path to a file containing a list of files to check. Everything else is ignored", default=None)

    args = parser.parse_args()

    config: LicensingConfig | None = None

    if (args.resources / "licensing.yml").exists():
        config = LicensingConfig(args.resources)
    else:
        print("USING EMPTY LICENSING CONFIG")
        config = LicensingConfig({})

    scan = LicensingScan(args.resources, config)
    scan.run()

    count = (len(scan.assets), len(scan.attributions))
    filtered_count = None

    if args.filelist:
        with open(args.filelist) as file:
            lines = file.readlines()
            filtered_count = scan.filter(set([x.strip() for x in lines]))

    validation = LicensingValidation(scan, config)
    if args.show_details is not None and args.hide_details is not None:
        print("Flags --hide-details (-d) and --show-details (-D) can not be used simultaneously")
        sys.exit(1)
    if args.ignore_flags is not None:
        validation.config.ignored_validation = args.ignore_flags
    if args.unattributed_stats is not None:
        validation.config.list_unattributed_stats = args.unattributed_stats

    validation.check()

    if filtered_count is None:
        print(f"Found {count[0]} assets, described by {count[1]} attributions.yml files")
    else:
        print(f"Found {filtered_count[0]} assets, described by {filtered_count[1]} attributions.yml files (filtered from {count[0]} assets and {count[1]} attributions.yml)")
    print()

    if len(validation.failures) > 0:
        fail_exit = False
        print("Assets attribution validation failed!")
        if len(validation.config.ignored_validation) > 0:
            print(f"Some checks have been ignored: {", ".join([e._name_ for e in sorted(validation.config.ignored_validation, key=lambda e: e.value)])}")
        print()

        for failure_type in ValidationFailure:
            if failure_type not in validation.failures:
                continue
            ignored = ""
            if failure_type not in validation.config.ignored_validation:
                fail_exit = True
            else:
                if args.hide_details is None and args.show_details is None:
                    continue
                ignored = " - IGNORED!"
            if args.show_details is not None and failure_type not in args.show_details:
                continue
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
