# CLAUDE.md

Claude Code bu dosyayı otomatik yükler; proje kuralları AGENTS.md'dedir:

@AGENTS.md

Ayrıntı ve okuma sırası: `README.md` → `docs/AI_WORKFLOW.md` → `docs/wiki/index.md`.

## Seviye
<!-- Bu bölümü proje başında doldurun (seviye tanımları: şablon deposundaki docs/SEVIYE_TANIMLARI.md); doldurulana kadar şablon varsayılanı geçerlidir. -->
Seviye: S3 (N-Tier) · Risk: yok (şablon varsayılanı, henüz görüşme yapılmadı)
Kapsam: <müşterinin asıl ihtiyacı>
İşin ihtiyaçları: e-posta bildirimi · içerik/görsel yükleme · deploy · yedek · spam koruması (her biri: var/yok/çözüm)
Yapmayacaklarımız: domain event, API versiyonlama, önbellek (Onion S4-S5; N-Tier'de yok, taşıma gerekir)
Seviye tavanı: N-Tier'in tavanı S3'tür; S4+ (domain event, API versiyonlama, önbellek) Onion şablonundadır ve geçiş reçete değil yeni projeye taşımadır.
