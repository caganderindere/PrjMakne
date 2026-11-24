"""Copies the company's own photos into content/images/gallery, downscaled for the web.

Usage: python tools/import_photos.py "<photo folder>"
Writes content/images/gallery/<category-slug>/NN.jpg, content/seed/media.json and the process-step
pictures in PrjProje/wwwroot/images/process. Indices below follow the sorted folder walk.
"""
import json
import os
import sys

from PIL import Image, ImageFile, ImageOps

ImageFile.LOAD_TRUNCATED_IMAGES = True
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAX_SIDE = 1600

# Source folder prefix -> gallery slug (Turkish product category slug; "tesisler" = plant installations).
FOLDERS = {
    "": "tesis-kurulumlari",
    "1-": "toz-boya-cihazlari", "CİHAZLAR": "toz-boya-cihazlari",
    "2-": "boya-uygulama-kabinleri", "3-": "boya-pisirme-firinlari", "5-": "konveyor-sistemleri",
    "6-": "ozel-imalat-urunleri", "7-": "kimyasal-yuzey-temizlik-hatlari", "8-": "siklonlar",
}
# Watermarked, CAD dimension drawings and spec tables stay out.
SKIP = {("konveyor-sistemleri", 2), ("konveyor-sistemleri", 13), ("kimyasal-yuzey-temizlik-hatlari", 1),
        ("toz-boya-cihazlari", 17), ("toz-boya-cihazlari", 18)}
COVERS = {"toz-boya-cihazlari": 11, "boya-uygulama-kabinleri": 14, "boya-pisirme-firinlari": 19,
          "konveyor-sistemleri": 0, "ozel-imalat-urunleri": 4, "kimyasal-yuzey-temizlik-hatlari": 20, "siklonlar": 2}
SERVICES = {"toz-boya-tesisleri": ("tesis-kurulumlari", 0), "elektrostatik-toz-boyama": ("boya-uygulama-kabinleri", 13),
            "lazer-kesim": ("ozel-imalat-urunleri", 6), "cnc-bukum": ("kimyasal-yuzey-temizlik-hatlari", 23)}
PROCESS = [("tesis-kurulumlari", 8), ("konveyor-sistemleri", 4), ("boya-pisirme-firinlari", 2),
           ("tesis-kurulumlari", 7), ("toz-boya-cihazlari", 11)]


def slug_for(name):
    if name in FOLDERS:
        return FOLDERS[name]
    return next((s for p, s in FOLDERS.items() if p and name.startswith(p)), None)


def images_in(path, recursive):
    found = []
    for dirpath, _, files in os.walk(path):
        found += [os.path.join(dirpath, f) for f in sorted(files) if f.lower().endswith((".jpg", ".jpeg", ".png", ".webp"))]
        if not recursive:
            break
    return found


def save(src, dst, max_side=MAX_SIDE):
    image = ImageOps.exif_transpose(Image.open(src)).convert("RGB")
    image.thumbnail((max_side, max_side))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    image.save(dst, "JPEG", quality=82, optimize=True, progressive=True)


def main(root):
    groups = {"tesis-kurulumlari": [("", images_in(root, False))]}
    for name in sorted(os.listdir(root)):
        full = os.path.join(root, name)
        if os.path.isdir(full) and slug_for(name):
            groups.setdefault(slug_for(name), []).append((name, images_in(full, True)))

    out = os.path.join(REPO, "content", "images", "gallery")
    picked = {}  # (slug, source index) -> relative path
    for slug, sources in groups.items():
        # Index per source folder (as in the review sheets), numbering per gallery slug.
        number = 0
        for folder, files in sources:
            offset = 0 if folder in ("", "CİHAZLAR") or slug != "toz-boya-cihazlari" else 1000
            for index, src in enumerate(files):
                if (slug, index + offset) in SKIP:
                    continue
                number += 1
                rel = f"gallery/{slug}/{number:03d}.jpg"
                save(src, os.path.join(REPO, "content", "images", rel))
                picked[(slug, index + offset)] = rel

    media = {
        "CategoryCovers": {slug: picked[(slug, i)] for slug, i in COVERS.items()},
        "ServiceImages": {key: picked[src] for key, src in SERVICES.items()},
        "Gallery": sorted({rel.split("/")[1] for rel in picked.values()}),
    }
    with open(os.path.join(REPO, "content", "seed", "media.json"), "w", encoding="utf-8") as f:
        json.dump(media, f, ensure_ascii=False, indent=2)

    process = os.path.join(REPO, "PrjProje", "wwwroot", "images", "process")
    for step, src in enumerate(PROCESS, 1):
        image = Image.open(os.path.join(REPO, "content", "images", picked[src]))
        image.thumbnail((800, 800))
        os.makedirs(process, exist_ok=True)
        image.save(os.path.join(process, f"step-{step}.webp"), "WEBP", quality=78)
    print(len(picked), "photos")


if __name__ == "__main__":
    main(sys.argv[1])
