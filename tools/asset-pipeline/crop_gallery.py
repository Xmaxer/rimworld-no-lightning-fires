"""Crops Workshop gallery images out of the dev-harness screenshots.

Refresh the screenshots first (RimWorld closed, Steam running):
    make smoke SCENARIOS=ui
then:
    python tools/asset-pipeline/crop_gallery.py
Output: workshop/gallery/*.png (upload these as Workshop screenshots).
Crop boxes assume the harness's 2560x1440 capture.
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SHOTS = ROOT / "tools" / ".smoke-profile" / "LightningHarness"
OUT = ROOT / "workshop" / "gallery"

# The ui scenario centres the camera on the strike, so a centred 16:9 box frames it.
CROPS = {
    "01-strike.png": ("ui-strike.png", (480, 270, 2080, 1170)),
    "02-aftermath.png": ("ui-aftermath.png", (480, 270, 2080, 1170)),
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (shot, box) in CROPS.items():
        src = SHOTS / shot
        if not src.exists():
            print(f"missing {src}, skipped")
            continue
        img = Image.open(src).convert("RGB")
        if img.size != (2560, 1440):
            print(f"{shot} is {img.size}, expected 2560x1440; crop boxes may be off")
        img.crop(box).save(OUT / name, optimize=True)
        print(OUT / name, img.crop(box).size)


if __name__ == "__main__":
    main()
