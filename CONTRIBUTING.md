# Contributing

Contributions are welcome! Issues and pull requests are accepted in **English or Turkish**.

## 🐛 Issues

When reporting a bug, include the template name (`my-ntier` or `my-onion`), the level/recipe if applicable, steps to reproduce, and the observed error output.

## 🔧 Pull requests

1. Fork the repository and create a branch from `main`.
2. Keep changes focused on a single fix or improvement.
3. Open a pull request with a concise summary of what changed and why.

## ✅ Verification before opening a PR

Run `dotnet build` and `dotnet test` from **each** template root (`1-NTier-Template/` and `2-Onion-Template/`), where `global.json` configures `Microsoft.Testing.Platform`:

```powershell
dotnet build ProjectApp.slnx
dotnet test ProjectApp.slnx -v q
```

If you have both templates installed locally (`dotnet new install`), run the smoke test from the repository root:

```powershell
pwsh ./tests/template-smoke.ps1
```

## 📦 Updating recipes

If you modify files inside a recipe under `recipes/`, regenerate its `degisiklikler.patch` and `onceki/` files using `tools/recete/patch_regen.py` and `tools/recete/add_onceki.py` (or `tools/recete/gen_recipe.py`), and verify the clean-copy build and tests with `tools/recete/verify.sh`. See [tools/recete/README.md](tools/recete/README.md) for details.
