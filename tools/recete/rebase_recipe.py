"""Reçeteyi değişmiş tabana taşır ve türetilmiş dosyalarını yeniden üretir.
Kullanım: python rebase_recipe.py <base_dir> <recipe_dir>
  base_dir : reçetenin uygulanacağı seviyenin GÜNCEL durumu (ör. şablon kaynağı ya da kaynak + önceki reçeteler).
Adımlar:
  1. files/ altındaki her dosya için taban, onceki/ (reçete hazırlanırkenki taban) ile farklıysa ve reçete dosyayı
     değiştiriyorsa: git merge-file ile reçete hâli yeni tabana taşınır (çakışma olursa durur, files/ değiştirilmez ve çakışanlar listelenir).
  2. dosyalar.txt files/ içeriğinden yeniden hesaplanır (tabanda yoksa A, varsa M; tabanla aynı olan dosya files/'tan çıkarılır).
  3. onceki/ = M dosyalarının güncel taban hâli; degisiklikler.patch yeniden üretilir.
Ortam: RG_TMP (geçici klasör)."""
import os, shutil, subprocess, sys, tempfile

base, recipe = sys.argv[1], sys.argv[2]
HERE = os.path.dirname(os.path.abspath(__file__))
files_dir = os.path.join(recipe, "files")
onceki_dir = os.path.join(recipe, "onceki")


def norm(p):
    t = open(p, "rb").read().decode("utf-8-sig")
    return t.replace("\r\n", "\n").rstrip()


def write_like(dst, text, like):
    raw = open(like, "rb").read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    if b"\r\n" in raw:
        text = text.replace("\r\n", "\n").replace("\n", "\r\n")
    open(dst, "wb").write((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


rels = []
for root, _, names in os.walk(files_dir):
    for n in names:
        rels.append(os.path.relpath(os.path.join(root, n), files_dir).replace("\\", "/"))
rels.sort()

conflicts, merged, results = [], [], {}
tmp = tempfile.mkdtemp(dir=os.environ.get("RG_TMP"))
try:
    for rel in rels:
        f, b, o = (os.path.join(d, rel) for d in (files_dir, base, onceki_dir))
        if not (os.path.isfile(b) and os.path.isfile(o)):
            continue
        if norm(b) == norm(o) or norm(f) == norm(b):
            continue
        # Taban değişmiş: ours = reçete hâli, base = eski taban, theirs = yeni taban
        paths = []
        for name, src in (("ours", f), ("base", o), ("theirs", b)):
            p = os.path.join(tmp, name)
            open(p, "w", encoding="utf-8", newline="\n").write(norm(src) + "\n")
            paths.append(p)
        r = subprocess.run(["git", "merge-file", "-L", "recete", "-L", "eski-taban", "-L", "yeni-taban", *paths])
        if r.returncode < 0:
            raise SystemExit(f"merge-file hata: {rel}")
        if r.returncode > 0:
            conflicts.append(rel)
        results[rel] = open(paths[0], encoding="utf-8").read()
        merged.append(rel)
finally:
    shutil.rmtree(tmp, ignore_errors=True)

if conflicts:
    print("ÇAKIŞMA (elle çöz, sonra tekrar çalıştır):", *conflicts, sep="\n  ")
    sys.exit(2)

for rel, text in results.items():
    f = os.path.join(files_dir, rel)
    write_like(f, text, f)

entries = []
for rel in rels:
    f, b = os.path.join(files_dir, rel), os.path.join(base, rel)
    if os.path.isfile(b):
        if norm(f) == norm(b):
            os.remove(f)  # reçete bu dosyayı değiştirmiyor
            continue
        entries.append(f"M\t{rel}")
    else:
        entries.append(f"A\t{rel}")
with open(os.path.join(recipe, "dosyalar.txt"), "w", encoding="utf-8", newline="\r\n") as fh:
    fh.write("\n".join(entries) + "\n")
for root, dirs, names in os.walk(files_dir, topdown=False):
    if not os.listdir(root):
        os.rmdir(root)

subprocess.run([sys.executable, os.path.join(HERE, "add_onceki.py"), base, recipe], check=True)
subprocess.run([sys.executable, os.path.join(HERE, "patch_regen.py"), base, recipe], check=True)
print(f"{recipe}: {len(entries)} dosya; tabana taşınan: {len(merged)} {merged}")
