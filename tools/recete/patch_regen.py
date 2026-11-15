"""Reçete degisiklikler.patch dosyasını dosyalar.txt'deki M satırlarından yeniden üretir.
Kullanım: python patch_regen.py <base_dir> <recipe_dir>
base_dir: reçeteden ÖNCEKİ seviye durumunun kökü. Hedef = recipe_dir/files.
"""
import os, shutil, subprocess, sys, tempfile

base, recipe = sys.argv[1], sys.argv[2]
files = os.path.join(recipe, "files")
out = []
with open(os.path.join(recipe, "dosyalar.txt"), encoding="utf-8-sig") as f:
    entries = [l.rstrip("\r\n").split("\t", 1) for l in f if l.strip()]
for status, path in entries:
    if status != "M":
        continue
    tmp = tempfile.mkdtemp(dir=os.environ["RG_TMP"])
    try:
        for side, src in (("a", os.path.join(base, path)), ("b", os.path.join(files, path))):
            dst = os.path.join(tmp, side, path)
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            shutil.copyfile(src, dst)
        r = subprocess.run(["git", "diff", "--no-index", "--no-color", "--", "a/" + path, "b/" + path], capture_output=True, cwd=tmp)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)
    if r.returncode not in (0, 1):
        raise SystemExit(r.stderr.decode())
    if not r.stdout:
        continue
    lines = r.stdout.split(b"\n")
    fixed = []
    for i, line in enumerate(lines):
        if line.startswith(b"diff --git "):
            line = f"diff --git a/{path} b/{path}".encode()
        elif line.startswith(b"--- a/") and i < 6:
            line = f"--- a/{path}".encode()
        elif line.startswith(b"+++ b/") and i < 6:
            line = f"+++ b/{path}".encode()
        fixed.append(line)
    out.append(b"\n".join(fixed).rstrip(b"\n") + b"\n")
with open(os.path.join(recipe, "degisiklikler.patch"), "wb") as f:
    f.write(b"".join(out))
print(recipe, "bölüm:", len(out))
