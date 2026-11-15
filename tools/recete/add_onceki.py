"""Reçeteye onceki/ klasörünü yazar: dosyalar.txt'deki her M dosyasının önceki seviyedeki (base) hâli.
Kullanım: python add_onceki.py <base_dir> <recipe_dir>"""
import os, shutil, sys

base, recipe = sys.argv[1], sys.argv[2]
out = os.path.join(recipe, "onceki")
shutil.rmtree(out, ignore_errors=True)
n = 0
with open(os.path.join(recipe, "dosyalar.txt"), encoding="utf-8-sig") as f:
    for line in f:
        parts = line.rstrip("\r\n").split("\t")
        if len(parts) < 2 or parts[0] != "M":
            continue
        src = os.path.join(base, parts[1])
        if not os.path.isfile(src):
            raise SystemExit(f"base'de yok: {src}")
        dst = os.path.join(out, parts[1])
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copyfile(src, dst)
        n += 1
print(f"{recipe}: onceki/ {n} dosya")
