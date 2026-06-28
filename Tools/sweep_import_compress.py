from pathlib import Path
import os

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    modified = 0
    checked = 0
    for path in ROOT.glob("Assets/**/*.import"):
        with open(path, "r", encoding="utf-8", errors="ignore") as handle:
            text = handle.read()
        checked += 1
        updated = text.replace("compress/mode=0", "compress/mode=2")
        if updated != text:
            with open(path, "w", encoding="utf-8", newline="") as handle:
                handle.write(updated)
            modified += 1
    print(f"checked={checked} modified={modified}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
