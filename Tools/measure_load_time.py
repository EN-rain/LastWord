from pathlib import Path
import subprocess
import time
import shutil

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    godot = shutil.which("godot") or shutil.which("godot4")
    if godot is None:
        print("godot_executable=missing")
        print("elapsed_seconds=unavailable")
        return 0

    start = time.perf_counter()
    proc = subprocess.run(
        [godot, "--headless", "--path", str(ROOT), "--quit-after", "3"],
        capture_output=True,
        text=True,
        timeout=30,
    )
    elapsed = time.perf_counter() - start
    print(f"elapsed_seconds={elapsed:.3f}")
    if proc.stderr:
        print(proc.stderr)
    return proc.returncode


if __name__ == "__main__":
    raise SystemExit(main())
