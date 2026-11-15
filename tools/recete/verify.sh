#!/bin/bash
# Kullanım: verify.sh <repo> <şablon klasörü> <reçete1> [reçete2...]  -> temiz kopyada patch --check, sonra files overlay, build, test
set -e
REPO="$1"; T="$2"; shift 2
W="$SCR/v-$T"; rm -rf "$W"; mkdir -p "$W"
cd "$REPO"; git ls-files -co --exclude-standard -z "$T" | xargs -0 -I{} cp --parents {} "$W"
cd "$W/$T"; git init -q; git config core.longpaths true; git config core.autocrlf false; git add -A; git -c user.email=x@x -c user.name=x commit -qm base
for r in "$@"; do
  git apply --check --ignore-whitespace "recipes/$r/degisiklikler.patch" || { echo "patch $r: HATA"; exit 1; }; echo "patch $r: OK"
  cp -r "recipes/$r/files/." .
  if [ -s "recipes/$r/silinecekler.txt" ]; then tr -d '\r' < "recipes/$r/silinecekler.txt" | grep -v '^#' | while read -r f; do [ -n "$f" ] && rm -rf "$f"; done; fi
  git add -A; git -c user.email=x@x -c user.name=x commit -qm "$r"
done
dotnet build --nologo -v q 2>&1 | grep -E "Uyarı|Hata|warning|error" | head -5
dotnet test --nologo -v q 2>&1 | grep -E "toplam|başarısız|başarılı:|error" | head -6
