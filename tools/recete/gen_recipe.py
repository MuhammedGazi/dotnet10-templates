"""Reçeteyi iki durum commit'inin farkından üretir.
Kullanım: python gen_recipe.py <work_repo> <from_ref> <to_ref> <recipe_dir>
files/ (A/M tam dosyalar), onceki/ (M dosyaların önceki hali), dosyalar.txt, silinecekler.txt ve degisiklikler.patch yeniden yazılır; README dokunulmaz."""
import os, shutil, subprocess, sys

W, FROM, TO, RECIPE = sys.argv[1:5]
HERE = os.path.dirname(os.path.abspath(__file__))
EXCL = [":!recipes", ":!docs", ":!*.md", ":!**/Migrations/**"]


def git(*a, cwd=W):
    r = subprocess.run(["git", *a], cwd=cwd, capture_output=True, text=True, encoding="utf-8")
    if r.returncode != 0:
        raise SystemExit(r.stderr)
    return r.stdout


def worktree(ref):
    d = os.path.join(os.environ["RG_TMP"], "wt-" + ref.replace("/", "_"))
    if os.path.exists(d):
        subprocess.run(["git", "worktree", "remove", "--force", d], cwd=W, capture_output=True)
        shutil.rmtree(d, ignore_errors=True)
    git("worktree", "add", "--detach", d, ref)
    return d


base, target = worktree(FROM), worktree(TO)
entries = [l.split("\t", 1) for l in git("diff", "--name-status", "--no-renames", FROM, TO, "--", ".", *EXCL).splitlines() if l]

files_dir = os.path.join(RECIPE, "files")
shutil.rmtree(files_dir, ignore_errors=True)
keep, deleted = [], []
for status, path in entries:
    if status == "D":
        deleted.append(path)
        continue
    src = os.path.join(target, path)
    dst = os.path.join(files_dir, path)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst)
    keep.append(f"{status}\t{path}")

keep.sort(key=lambda l: l.split("\t", 1)[1])
with open(os.path.join(RECIPE, "dosyalar.txt"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write("\n".join(keep) + "\n")
with open(os.path.join(RECIPE, "silinecekler.txt"), "w", encoding="utf-8", newline="\r\n") as f:
    f.write("".join(p + "\n" for p in sorted(deleted)))

subprocess.run([sys.executable, os.path.join(HERE, "patch_regen.py"), base, RECIPE], check=True)
subprocess.run([sys.executable, os.path.join(HERE, "add_onceki.py"), base, RECIPE], check=True)
print(f"{RECIPE}: {len(keep)} dosya, {len(deleted)} silinecek")
