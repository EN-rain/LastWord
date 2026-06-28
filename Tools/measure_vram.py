from pathlib import Path
import os

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    total = 0
    for path in ROOT.glob("Assets/**/*"):
        if path.is_file() and path.suffix.lower() in {".png", ".jpg", ".jpeg", ".glb", ".gltf", ".bin"}:
            total += path.stat().st_size
    print(f"asset_bytes={total}")
    print(f"asset_megabytes={total / (1024 * 1024):.2f}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
