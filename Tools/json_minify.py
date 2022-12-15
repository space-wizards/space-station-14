from json import dump, load
from json.decoder import JSONDecodeError
from pathlib import Path
from glob import iglob


def main():
    resources: Path = Path("../Resources")
    a = resources.resolve()
    errors = []

    for fn in a.rglob("**/meta.json"):
        try:
            # Read and write are separate so we can fix file formats
            with fn.open("r", encoding="utf-8-sig") as f:
                loaded = load(f)
            with fn.open("w", encoding="utf-8") as f:
                dump(loaded, f, separators=(",", ":"))
        except JSONDecodeError:
            errors.append(fn)

        print(f"Minified {fn}")

    for error in errors:
        print(f"JSON decode error minifying {error}")


if __name__ == "__main__":
    main()
