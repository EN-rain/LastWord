from pathlib import Path
import os
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets" / "KayKitFurniture" / "gltf"
OUT = ROOT / "Assets" / "KayKitFurniture" / "glb"
LOG = ROOT / "Tools" / "convert_log.txt"


def main() -> int:
    subprocess.run(
        ["powershell", "-NoProfile", "-Command", f"New-Item -ItemType Directory -Force -Path '{OUT}' | Out-Null"],
        check=False,
    )
    lines = []
    converted = 0
    skipped = 0

    for gltf in sorted(SRC.glob("*.gltf")):
        target = OUT / f"{gltf.stem}.glb"
        if target.exists():
            skipped += 1
            continue

        try:
            import pygltflib  # type: ignore
            model = pygltflib.GLTF2().load(str(gltf))
            with tempfile.NamedTemporaryFile(suffix=".glb", delete=False) as temp:
                temp_path = Path(temp.name)
            model.save_binary(str(temp_path))
            move = subprocess.run(
                [
                    "powershell",
                    "-NoProfile",
                    "-Command",
                    f"Move-Item -Force -LiteralPath '{temp_path}' -Destination '{target}'",
                ],
                capture_output=True,
                text=True,
            )
            if move.returncode != 0:
                raise OSError(move.stderr.strip() or move.stdout.strip() or "PowerShell Move-Item failed")
            converted += 1
        except Exception as exc:
            lines.append(f"{gltf}: pygltflib conversion failed: {exc}")

    try:
        with open(LOG, "w", encoding="utf-8") as handle:
            handle.write("\n".join(lines))
    except OSError as exc:
        print(f"warning: could not write {LOG}: {exc}", file=sys.stderr)
        for line in lines[:10]:
            print(line, file=sys.stderr)
    print(f"converted={converted} skipped={skipped} failures={len(lines)}")
    return 0 if not lines else 1


if __name__ == "__main__":
    raise SystemExit(main())
