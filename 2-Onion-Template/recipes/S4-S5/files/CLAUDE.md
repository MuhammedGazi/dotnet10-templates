# CLAUDE.md

Claude Code bu dosyayı otomatik yükler; proje kuralları AGENTS.md'dedir:

@AGENTS.md

Ayrıntı ve okuma sırası: `README.md` → `docs/AI_WORKFLOW.md` → `docs/SYSTEM_AUDIT.md` → `docs/wiki/index.md` → `recipes/README.md`.

## Seviye
<!-- Bu bölümü proje başında doldurun (seviye tanımları: şablon deposundaki docs/SEVIYE_TANIMLARI.md); doldurulana kadar şablon varsayılanı geçerlidir. -->
Seviye: S5 (Onion) · Risk: yok (şablon varsayılanı, henüz görüşme yapılmadı)
Kapsam: <müşterinin asıl ihtiyacı>
İşin ihtiyaçları: e-posta bildirimi · içerik/görsel yükleme · deploy · yedek · spam koruması (her biri: var/yok/çözüm)
Yapmayacaklarımız: Redis/outbox/OpenTelemetry (ölçülmüş ihtiyaç olmadan) · çok kiracılık (şablonda yok; proje kararı) · Dapper (arşiv)
Seviye atlama: S5 en üst şablon seviyesidir; ötesi reçete değil mimari karardır.
