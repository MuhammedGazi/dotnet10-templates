<#
.SYNOPSIS
    Seviye reçetesini (ör. S1-S2) projeye güvenle uygular; projede değiştirdiğin dosyaları otomatik birleştirir.

.DESCRIPTION
    dosyalar.txt'deki her dosya için üç sürüm karşılaştırılır (satır sonu ve BOM farkı yok sayılır):
      proje  = projedeki hâl, onceki = şablonun önceki seviyedeki hâli (onceki/), recete = hedef seviyedeki hâl (files/)
    Sonuç:
      KOPYALA   : dosyaya dokunulmamış (proje = onceki) ya da yeni dosya -> reçetedeki hâl kopyalanır.
      BIRLESIR  : dosyayı sen de değiştirmişsin; değişiklikler çakışmıyor -> git merge-file ile otomatik birleştirilir.
      CAKISMA   : sen ve reçete aynı satırlara dokunmuşsunuz -> dosya DEĞİŞTİRİLMEZ; reçete farkını elle aktar (git diff --no-index).
      AYNI      : reçete bu dosyaya zaten uygulanmış -> işlem yok.
      VAR       : reçetenin ekleyeceği yeni dosya projede farklı içerikle zaten var -> elle incele.
      EKSIK     : değişecek dosya projede yok (silinmiş/taşınmış) -> elle incele.
      ELLE      : appsettings*.json ve CLAUDE.md -> asla değiştirilmez; appsettings anahtarlarını README'den, CLAUDE.md'yi reçeteyle karşılaştırarak elle güncelle.
    Varsayılan çalışma yalnızca rapor verir (birleştirmeyi deneyip sonucunu söyler, dosya yazmaz).
    -Uygula: KOPYALA/BIRLESIR satırlarını yazar ve silinecekler.txt'yi uygular. Temiz git ağacı ister (önce commit'le).
    Bir tane bile CAKISMA varsa -Uygula HİÇBİR dosya yazmaz ve çıkış kodu 1 olur (yarım yükseltme kalmaz):
    çakışan dosyaları önce elle reçeteye uydur (commit'le), sonra yeniden çalıştır.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S1-S2
    powershell -ExecutionPolicy Bypass -File recipes/recete-uygula.ps1 -Recete S1-S2 -Uygula
#>
param(
    [Parameter(Mandatory = $true)] [string] $Recete,
    [switch] $Uygula
)

$ErrorActionPreference = 'Stop'
# Türkçe karakterler Windows PowerShell 5.1 konsolunda bozulmasın.
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }
$projeKoku = Split-Path -Parent $PSScriptRoot
$receteKlasoru = Join-Path $PSScriptRoot $Recete
$liste = Join-Path $receteKlasoru 'dosyalar.txt'
if (-not (Test-Path $liste)) { throw "Reçete bulunamadı: $receteKlasoru" }
if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'git bulunamadı (birleştirme için gerekli).' }

if ($Uygula) {
    $ErrorActionPreference = 'Continue'
    & git -C $projeKoku rev-parse --is-inside-work-tree 2>$null | Out-Null
    $gitKodu = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($gitKodu -ne 0) { throw 'Proje bir git deposu değil; -Uygula geri alınabilir olmalı: önce git init + commit.' }
    $kirli = & git -C $projeKoku status --porcelain
    if ($kirli) { throw 'Çalışma ağacı temiz değil; önce değişikliklerini commit''le (git status).' }
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$gecici = Join-Path ([System.IO.Path]::GetTempPath()) ("recete-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $gecici | Out-Null

function Get-Normal([string] $yol) {
    # ReadAllText BOM'u atar; satır sonları ve sondaki boşluklar eşitlenir.
    return [System.IO.File]::ReadAllText($yol).Replace("`r`n", "`n").TrimEnd()
}

function Invoke-Birlestir([string] $proje, [string] $once, [string] $yeni) {
    # Üç sürümü LF'e çevirip git merge-file ile birleştirir; sonucu projenin satır sonu ve BOM'uyla döndürür.
    $p = Join-Path $gecici 'proje'; $o = Join-Path $gecici 'onceki'; $r = Join-Path $gecici 'recete'
    [System.IO.File]::WriteAllText($p, (Get-Normal $proje) + "`n", $utf8)
    [System.IO.File]::WriteAllText($o, (Get-Normal $once) + "`n", $utf8)
    [System.IO.File]::WriteAllText($r, (Get-Normal $yeni) + "`n", $utf8)
    # -p kullanılmaz: konsol kod sayfası Türkçe karakterleri bozar. Sonuç $p dosyasına yazılır, UTF-8 okunur.
    & git merge-file -L proje -L onceki -L recete $p $o $r | Out-Null
    $cakisma = $LASTEXITCODE
    if ($cakisma -lt 0) { throw "git merge-file hata verdi: $proje" }
    $metin = [System.IO.File]::ReadAllText($p, $utf8)
    $ham = [System.IO.File]::ReadAllBytes($proje)
    if ([System.Text.Encoding]::UTF8.GetString($ham).Contains("`r`n")) { $metin = $metin.Replace("`n", "`r`n") }
    $bom = $ham.Length -ge 3 -and $ham[0] -eq 0xEF -and $ham[1] -eq 0xBB -and $ham[2] -eq 0xBF
    return [pscustomobject]@{ Metin = $metin; Cakisma = $cakisma; Bom = $bom }
}

$satirlar = @()
foreach ($satir in [System.IO.File]::ReadAllLines($liste)) {
    if (-not $satir.Trim()) { continue }
    $parca = $satir.Split("`t")
    $durum = $parca[0]
    $yol = $parca[1]
    $hedef = Join-Path $projeKoku $yol
    $yeni = Join-Path $receteKlasoru (Join-Path 'files' $yol)
    $once = Join-Path $receteKlasoru (Join-Path 'onceki' $yol)
    $birlesim = $null

    $leaf = Split-Path -Leaf $yol
    if ($leaf -like 'appsettings*.json' -or $leaf -eq 'CLAUDE.md') { $islem = 'ELLE' }
    elseif (-not (Test-Path $hedef)) { $islem = if ($durum -eq 'A') { 'KOPYALA' } else { 'EKSIK' } }
    elseif ((Get-Normal $hedef) -eq (Get-Normal $yeni)) { $islem = 'AYNI' }
    elseif ($durum -eq 'A') { $islem = 'VAR' }
    elseif (-not (Test-Path $once)) { $islem = 'EKSIK' }
    elseif ((Get-Normal $hedef) -eq (Get-Normal $once)) { $islem = 'KOPYALA' }
    else {
        $birlesim = Invoke-Birlestir $hedef $once $yeni
        $islem = if ($birlesim.Cakisma -eq 0) { 'BIRLESIR' } else { 'CAKISMA' }
    }

    $satirlar += [pscustomobject]@{
        Islem = $islem; Tur = $durum; Dosya = $yol; Kaynak = $yeni; Hedef = $hedef; Birlesim = $birlesim
        Not = if ($birlesim -and $birlesim.Cakisma -gt 0) { "$($birlesim.Cakisma) çakışma" } else { '' }
    }
}

$sira = @{ CAKISMA = 0; VAR = 1; EKSIK = 2; ELLE = 3; BIRLESIR = 4; KOPYALA = 5; AYNI = 6 }
$satirlar | Sort-Object { $sira[$_.Islem] }, Dosya | Format-Table Islem, Tur, Dosya, Not -AutoSize | Out-Host

$silinecekler = Join-Path $receteKlasoru 'silinecekler.txt'
$silinecekListe = @()
if (Test-Path $silinecekler) {
    $silinecekListe = @([System.IO.File]::ReadAllLines($silinecekler) | Where-Object { $_.Trim() -and -not $_.StartsWith('#') })
}

$cakismaSayisi = @($satirlar | Where-Object { $_.Islem -eq 'CAKISMA' }).Count
$durdur = $Uygula -and $cakismaSayisi -gt 0

if ($durdur) {
    Write-Host "UYGULANMADI: $cakismaSayisi dosyada çakışma var; hiçbir dosya yazılmadı, hiçbir dosya silinmedi."
    Write-Host 'Çakışan dosyaları aşağıdaki komutla elle reçeteye uydur, commit''le ve -Uygula ile yeniden çalıştır.'
}
elseif ($Uygula) {
    $yazilan = 0
    foreach ($s in $satirlar) {
        if ($s.Islem -eq 'KOPYALA') {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $s.Hedef) | Out-Null
            Copy-Item -Force $s.Kaynak $s.Hedef
            $yazilan++
        }
        elseif ($s.Islem -eq 'BIRLESIR') {
            [System.IO.File]::WriteAllText($s.Hedef, $s.Birlesim.Metin, (New-Object System.Text.UTF8Encoding($s.Birlesim.Bom)))
            $yazilan++
        }
    }
    foreach ($sil in $silinecekListe) {
        $p = Join-Path $projeKoku $sil
        if (Test-Path $p) { Remove-Item -Force -Recurse $p }
    }
    Write-Host ("Yazılan: {0} (kopyalanan + birleştirilen), silinen: {1}" -f $yazilan, $silinecekListe.Count)
}
else {
    Write-Host 'Yalnızca rapor (dosya yazılmadı). Uygulamak için -Uygula ekle; önce değişikliklerini commit''le.'
}

$elle = @($satirlar | Where-Object { $_.Islem -in 'CAKISMA', 'VAR', 'EKSIK', 'ELLE' })
if ($elle.Count -gt 0) {
    Write-Host ''
    Write-Host "Elle bakılacak $($elle.Count) dosya:"
    foreach ($s in $elle) {
        switch ($s.Islem) {
            'CAKISMA' { Write-Host "  CAKISMA  $($s.Dosya): dosya değiştirilmedi; reçete farkını elle aktar: git diff --no-index recipes/$Recete/onceki/$($s.Dosya) recipes/$Recete/files/$($s.Dosya)" }
            'VAR'     { Write-Host "  VAR      $($s.Dosya): reçetenin yeni dosyası projede farklı içerikle var; recipes/$Recete/files/ ile karşılaştır." }
            'EKSIK'   { Write-Host "  EKSIK    $($s.Dosya): dosya projede yok; taşıdıysan recipes/$Recete/onceki/ -> files/ farkını yeni yerine aktar." }
            'ELLE'    {
                if ((Split-Path -Leaf $s.Dosya) -eq 'CLAUDE.md') { Write-Host "  ELLE     $($s.Dosya): Seviye: satırını hedef seviyeye çek, Yapmayacaklarımız satırını recipes/$Recete/files/CLAUDE.md ile karşılaştırıp güncelle (dosya kopyalanmaz)." }
                else { Write-Host "  ELLE     $($s.Dosya): README'deki anahtarları ekle (dosya kopyalanmaz)." }
            }
        }
    }
}

Remove-Item -Recurse -Force $gecici -ErrorAction SilentlyContinue
if ($durdur) { exit 1 }
