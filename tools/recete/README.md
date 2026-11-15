# Reçete araçları (şablon bakımı için; şablonlarla dağıtılmaz)

Reçeteler iki seviye durumunun farkından üretilir. Durumlar ayrı bir git reposunda commit olarak tutulur
(ör. Onion: S3 → S4 → S5), sonra:

| Betik | Ne yapar |
|---|---|
| `gen_recipe.py <repo> <from> <to> <recete_klasoru>` | `files/`, `onceki/`, `dosyalar.txt`, `silinecekler.txt`, `degisiklikler.patch` üretir (README'ye dokunmaz) |
| `patch_regen.py <base_dir> <recete_klasoru>` | Yalnızca `degisiklikler.patch`'i `dosyalar.txt`'deki M satırlarından yeniden üretir |
| `add_onceki.py <base_dir> <recete_klasoru>` | Yalnızca `onceki/`'yi yeniden yazar |
| `verify.sh <repo_koku> <sablon> [recete...]` | Şablonun temiz kopyasına reçeteleri sırayla uygular (`git apply --check` + kopyalama), build ve test |

Ortam: `RG_TMP` (geçici çalışma klasörü) ve `verify.sh` için `SCR` ortam değişkenleri. Windows'ta uzun yollar için
`git config core.longpaths true`. Reçete değiştiğinde ilgili README'deki test sayısını güncelle ve
`tests/template-smoke.ps1` çalıştır.
