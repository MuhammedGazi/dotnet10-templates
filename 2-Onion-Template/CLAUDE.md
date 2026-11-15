# CLAUDE.md

Claude Code bu dosyayı otomatik yükler; proje kuralları AGENTS.md'dedir:

@AGENTS.md

Ayrıntı ve okuma sırası: `README.md` → `docs/AI_WORKFLOW.md` → `docs/SYSTEM_AUDIT.md` → `docs/wiki/index.md` → `recipes/README.md`.

## Seviye
<!-- Bu bölümü proje başında doldurun (seviye tanımları: şablon deposundaki docs/SEVIYE_TANIMLARI.md); doldurulana kadar şablon varsayılanı geçerlidir. -->
Seviye: S3 (Onion) · Risk: yok (şablon varsayılanı, henüz görüşme yapılmadı)
Kapsam: <müşterinin asıl ihtiyacı>
İşin ihtiyaçları: e-posta bildirimi · içerik/görsel yükleme · deploy · yedek · spam koruması (her biri: var/yok/çözüm)
Yapmayacaklarımız: domain event/bus ve API versiyonlama (S4) · önbellek, dış kuyruk (S5) · çok kiracılık (şablonda yok; gerekiyorsa S4'e geçmeden proje kararı) · Dapper (arşiv)
Seviye atlama: `recipes/` altındaki reçeteyle; sıfırdan yazılmaz.
