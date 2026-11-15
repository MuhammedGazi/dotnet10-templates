<#
.SYNOPSIS
    Template üretim testi: iki template'ten farklı proje adlarıyla çözüm üretir,
    'ProjectApp' kalıntısı olmadığını, build ve testlerin yeşil olduğunu doğrular.

.DESCRIPTION
    Template'lerin 'dotnet new install <klasör>' ile kurulu olduğu varsayılır
    (klasörden kurulum kaynağı canlı okur, yeniden kurmak gerekmez).
    Çıktılar geçici klasöre yazılır ve iş bitince silinir.

.EXAMPLE
    pwsh ./tests/template-smoke.ps1
    pwsh ./tests/template-smoke.ps1 -Names my-shop -Templates my-onion -KeepOutput
#>
param(
    [string[]] $Templates = @('my-ntier', 'my-onion'),
    [string[]] $Names = @('ShopApi', 'Shop.Api', 'my_app', 'my-shop'),
    [string] $WorkRoot = (Join-Path ([System.IO.Path]::GetTempPath()) 'TemplateSmoke'),
    [switch] $SkipTests,
    [switch] $KeepOutput
)

$ErrorActionPreference = 'Stop'
$results = @()

if (Test-Path $WorkRoot) { Remove-Item $WorkRoot -Recurse -Force }
New-Item -ItemType Directory -Path $WorkRoot | Out-Null

foreach ($template in $Templates) {
    foreach ($name in $Names) {
        $outDir = Join-Path $WorkRoot "$template-$($name -replace '[^A-Za-z0-9]', '_')"
        $row = [ordered]@{ Template = $template; Name = $name; Generate = '-'; Leftover = '-'; Build = '-'; Test = '-'; Port = '-'; KeyHash = '-' }

        dotnet new $template -n $name -o $outDir --skip-restore | Out-Null
        if ($LASTEXITCODE -ne 0) { $row.Generate = 'FAIL'; $results += [pscustomobject]$row; continue }
        $row.Generate = 'OK'

        # İçerikte veya dosya/klasör adında 'ProjectApp' kalmamalı
        $contentHits = Get-ChildItem $outDir -Recurse -File | Select-String -Pattern 'ProjectApp' -List
        $nameHits = Get-ChildItem $outDir -Recurse | Where-Object { $_.Name -like '*ProjectApp*' }
        $row.Leftover = if ($contentHits -or $nameHits) { "FAIL ($(@($contentHits).Count + @($nameHits).Count))" } else { 'OK' }

        # Her üretimde port ve geliştirme SigningKey'i farklı olmalı (template sembolleri)
        $launch = Get-ChildItem $outDir -Recurse -Filter launchSettings.json | Select-Object -First 1
        if ($launch -and ((Get-Content $launch.FullName -Raw) -match 'http://localhost:(\d+)"')) { $row.Port = $Matches[1] }
        $devSettings = Get-ChildItem $outDir -Recurse -Filter appsettings.Development.json | Select-Object -First 1
        if ($devSettings) {
            $key = (Get-Content $devSettings.FullName -Raw | ConvertFrom-Json).Jwt.SigningKey
            $row.KeyHash = $key.GetHashCode().ToString('X8')
        }

        $sln = Get-ChildItem $outDir -Filter *.slnx | Select-Object -First 1
        dotnet build $sln.FullName -v q -nologo | Out-Null
        $row.Build = if ($LASTEXITCODE -eq 0) { 'OK' } else { 'FAIL' }

        if (-not $SkipTests -and $row.Build -eq 'OK') {
            # global.json (test runner = Microsoft.Testing.Platform) çalışma klasöründen okunduğu için proje kökünde çalıştırılır.
            Push-Location $outDir
            try { $testOutput = dotnet test --solution $sln.FullName --no-build 2>&1 | Out-String } finally { Pop-Location }
            $row.Test = if ($LASTEXITCODE -eq 0) { 'OK' } else { 'FAIL' }
            $passed = [regex]::Matches($testOutput, '(?im)^\s*(?:başarılı|passed|succeeded):\s+(\d+)') | ForEach-Object { [int]$_.Groups[1].Value }
            if ($passed) { $row.Test += " ($(($passed | Measure-Object -Sum).Sum))" }
        }

        $results += [pscustomobject]$row
    }
}

$results | Format-Table -AutoSize

if (-not $KeepOutput) { Remove-Item $WorkRoot -Recurse -Force }

$failed = @($results | Where-Object { $_.Generate -ne 'OK' -or $_.Leftover -ne 'OK' -or $_.Build -ne 'OK' -or ($_.Test -like 'FAIL*') })

$duplicateKeys = $results | Group-Object KeyHash | Where-Object { $_.Count -gt 1 -and $_.Name -ne '-' }
if ($duplicateKeys) { Write-Warning 'Aynı geliştirme SigningKey birden fazla projede üretildi.'; $failed += $duplicateKeys }
if ($failed) {
    Write-Error "$(@($failed).Count) kombinasyon başarısız."
    exit 1
}
Write-Host 'Tüm kombinasyonlar başarılı.'
