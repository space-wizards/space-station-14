#!/usr/bin/env python3
"""Synchronize Corvax TTS voice prototypes with the Silero voice catalog.

The script is a dry-run unless --write is supplied. Existing voices which are
missing from the remote catalog are intentionally preserved: entity prototypes
may still refer to their prototype IDs. In SD-only mode, voices explicitly
marked by the catalog as HD-only are removed.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import tempfile
import unicodedata
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable


DEFAULT_CATALOG_URL = "https://tts-bot-web.silero.ai/list.json"
REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_PROTOTYPES = REPOSITORY_ROOT / "Resources/Prototypes/_Corvax/tts-voices.yml"
DEFAULT_LOCALE = REPOSITORY_ROOT / "Resources/Locale/ru-RU/corvax/tts/tts-voices.ftl"


@dataclass(frozen=True)
class Prototype:
    block: str
    prototype_id: str
    name_key: str
    speaker: str


def parse_arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--include-hd",
        action="store_true",
        help="include voices with hd: 1; the default includes only sd: 1",
    )
    parser.add_argument(
        "--write",
        action="store_true",
        help="write the generated prototype and localization files",
    )
    parser.add_argument(
        "--catalog",
        default=DEFAULT_CATALOG_URL,
        help="catalog URL or path to a downloaded list.json",
    )
    parser.add_argument("--prototypes", type=Path, default=DEFAULT_PROTOTYPES)
    parser.add_argument("--locale", type=Path, default=DEFAULT_LOCALE)
    return parser.parse_args()


def read_catalog(source: str) -> list[dict[str, Any]]:
    local_path = Path(source)
    if local_path.is_file():
        raw = local_path.read_bytes()
    else:
        request = urllib.request.Request(source, headers={"User-Agent": "ds14-tts-updater/1.0"})
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read()

    catalog = json.loads(raw.decode("utf-8-sig"))
    if not isinstance(catalog, list):
        raise ValueError("TTS catalog root must be a JSON array")
    return catalog


def parse_prototypes(text: str) -> list[Prototype]:
    normalized = text.replace("\r\n", "\n").strip()
    blocks = re.split(r"\n[ \t]*\n", normalized) if normalized else []
    prototypes: list[Prototype] = []

    for block in blocks:
        if not re.search(r"(?m)^- type: ttsVoice\s*$", block):
            raise ValueError(f"Unexpected block in TTS prototype file:\n{block[:120]}")
        prototype_id = required_field(block, "id")
        name_key = required_field(block, "name")
        speaker = unquote_yaml_scalar(required_field(block, "speaker"))
        prototypes.append(Prototype(block, prototype_id, name_key, speaker))

    ensure_unique((voice.prototype_id for voice in prototypes), "prototype ID")
    ensure_unique((voice.name_key for voice in prototypes), "localization key")
    ensure_unique((voice.speaker for voice in prototypes), "speaker")
    return prototypes


def required_field(block: str, field: str) -> str:
    match = re.search(rf"(?m)^  {re.escape(field)}:\s*(.+?)\s*$", block)
    if match is None:
        raise ValueError(f"TTS prototype has no {field!r} field:\n{block}")
    return match.group(1)


def unquote_yaml_scalar(value: str) -> str:
    if value.startswith('"'):
        return json.loads(value)
    return value


def ensure_unique(values: Iterable[str], description: str) -> None:
    seen: set[str] = set()
    duplicates: set[str] = set()
    for value in values:
        if value in seen:
            duplicates.add(value)
        seen.add(value)
    if duplicates:
        raise ValueError(f"Duplicate {description}s: {', '.join(sorted(duplicates))}")


def canonical_catalog(catalog: list[dict[str, Any]]) -> tuple[dict[str, dict[str, Any]], list[str]]:
    grouped: dict[str, list[dict[str, Any]]] = {}
    for row in catalog:
        handle = str(row.get("api_handle") or "").strip()
        if handle:
            grouped.setdefault(handle, []).append(row)

    canonical: dict[str, dict[str, Any]] = {}
    warnings: list[str] = []
    for handle, rows in grouped.items():
        if len(rows) == 1:
            canonical[handle] = rows[0]
            continue

        exact = [row for row in rows if str(row.get("bot_handle") or "") == handle]
        if len(exact) == 1:
            canonical[handle] = exact[0]
            warnings.append(f"duplicate {handle!r}: selected row whose bot_handle matches api_handle")
            continue

        aliases = ", ".join(repr(str(row.get("alias") or "")) for row in rows)
        warnings.append(f"ambiguous duplicate {handle!r} skipped ({aliases})")

    return canonical, warnings


def is_enabled(row: dict[str, Any], include_hd: bool) -> bool:
    return int(row.get("sd") or 0) == 1 or (include_hd and int(row.get("hd") or 0) == 1)


def is_hd_only(row: dict[str, Any]) -> bool:
    return int(row.get("sd") or 0) == 0 and int(row.get("hd") or 0) == 1


def make_identifier(row: dict[str, Any], handle: str) -> str:
    preferred = str(row.get("bot_handle") or row.get("display_name") or handle)
    ascii_name = unicodedata.normalize("NFKD", preferred).encode("ascii", "ignore").decode("ascii")
    words = re.findall(r"[A-Za-z0-9]+", ascii_name)
    identifier = "".join(word[:1].upper() + word[1:] for word in words)
    if not identifier:
        identifier = "Voice" + hashlib.sha1(handle.encode("utf-8")).hexdigest()[:10].upper()
    if identifier[0].isdigit():
        identifier = "Voice" + identifier
    return identifier


def allocate_identifier(base: str, used: set[str]) -> str:
    candidate = base
    suffix = 2
    while candidate.casefold() in used:
        candidate = f"{base}{suffix}"
        suffix += 1
    used.add(candidate.casefold())
    return candidate


def voice_sex(row: dict[str, Any]) -> str:
    value = str(row.get("sex") or "").strip().casefold()
    if value in {"м", "m", "male"}:
        return "Male"
    if value in {"ж", "f", "female"}:
        return "Female"
    return "Unsexed"


def fluent_value(row: dict[str, Any], fallback: str) -> str:
    value = str(row.get("alias") or row.get("display_name") or fallback)
    value = " ".join(value.replace("{", "(").replace("}", ")").splitlines()).strip()
    return value or fallback


def render_prototype(prototype_id: str, name_key: str, handle: str, sex: str) -> str:
    quoted_handle = json.dumps(handle, ensure_ascii=False)
    return (
        "- type: ttsVoice\n"
        f"  id: {prototype_id}\n"
        f"  name: {name_key}\n"
        f"  sex: {sex}\n"
        f"  speaker: {quoted_handle}"
    )


def detect_newline(raw: bytes) -> str:
    return "\r\n" if b"\r\n" in raw else "\n"


def atomic_write(path: Path, text: str, newline: str) -> None:
    data = text.replace("\n", newline).encode("utf-8")
    descriptor, temporary_name = tempfile.mkstemp(prefix=path.name + ".", dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as temporary:
            temporary.write(data)
        os.replace(temporary_name, path)
    except BaseException:
        try:
            os.unlink(temporary_name)
        except FileNotFoundError:
            pass
        raise


def main() -> int:
    args = parse_arguments()
    prototype_raw = args.prototypes.read_bytes()
    locale_raw = args.locale.read_bytes()
    prototypes = parse_prototypes(prototype_raw.decode("utf-8-sig"))
    catalog, warnings = canonical_catalog(read_catalog(args.catalog))

    existing_by_speaker = {voice.speaker: voice for voice in prototypes}
    removed = [
        voice
        for voice in prototypes
        if not args.include_hd
        and voice.speaker in catalog
        and is_hd_only(catalog[voice.speaker])
    ]
    removed_speakers = {voice.speaker for voice in removed}
    kept = [voice for voice in prototypes if voice.speaker not in removed_speakers]

    selected_rows = [
        (handle, row)
        for handle, row in catalog.items()
        if is_enabled(row, args.include_hd) and handle not in existing_by_speaker
    ]
    selected_rows.sort(key=lambda item: (float(item[1].get("rank") or 1e12), item[0].casefold()))

    used_ids = {voice.prototype_id.casefold() for voice in kept}
    used_keys = {voice.name_key for voice in kept}
    additions: list[tuple[str, str, str, str]] = []
    for handle, row in selected_rows:
        prototype_id = allocate_identifier(make_identifier(row, handle), used_ids)
        name_key = f"tts-voice-name-{prototype_id.casefold()}"
        suffix = 2
        while name_key in used_keys:
            name_key = f"tts-voice-name-{prototype_id.casefold()}-{suffix}"
            suffix += 1
        used_keys.add(name_key)
        additions.append((prototype_id, name_key, handle, fluent_value(row, prototype_id)))

    output_blocks = [voice.block for voice in kept]
    output_blocks.extend(
        render_prototype(prototype_id, name_key, handle, voice_sex(catalog[handle]))
        for prototype_id, name_key, handle, _ in additions
    )
    prototype_output = "\n\n".join(output_blocks) + "\n"

    removed_keys = {voice.name_key for voice in removed}
    locale_lines = locale_raw.decode("utf-8-sig").replace("\r\n", "\n").splitlines()
    locale_lines = [
        line
        for line in locale_lines
        if not any(re.match(rf"^{re.escape(key)}\s*=", line) for key in removed_keys)
    ]
    if locale_lines and locale_lines[-1]:
        locale_lines.append("")
    locale_lines.extend(f"{name_key} = {alias}" for _, name_key, _, alias in additions)
    locale_output = "\n".join(locale_lines) + "\n"

    stale = [voice for voice in kept if voice.speaker not in catalog]
    mode = "SD + HD" if args.include_hd else "SD only"
    print(f"Mode: {mode}")
    print(f"Existing: {len(prototypes)}")
    print(f"Add: {len(additions)}")
    print(f"Remove HD-only: {len(removed)}")
    print(f"Preserve missing from catalog: {len(stale)}")
    print(f"Result: {len(kept) + len(additions)}")
    if removed:
        print("Removed: " + ", ".join(f"{v.prototype_id} ({v.speaker})" for v in removed))
    for warning in warnings:
        print("Warning: " + warning)

    # Validate generated content before replacing either file.
    parse_prototypes(prototype_output)
    locale_keys = [
        match.group(1)
        for line in locale_output.splitlines()
        if (match := re.match(r"^(tts-voice-name-[^ ]+)\s*=", line))
    ]
    ensure_unique(locale_keys, "localization key")

    if args.write:
        atomic_write(args.prototypes, prototype_output, detect_newline(prototype_raw))
        atomic_write(args.locale, locale_output, detect_newline(locale_raw))
        print("Updated prototype and localization files.")
    else:
        print("Dry-run only; pass --write to update files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
