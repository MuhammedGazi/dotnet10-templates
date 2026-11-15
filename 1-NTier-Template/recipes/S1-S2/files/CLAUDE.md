# CLAUDE.md

Claude Code bu dosyayı otomatik yükler; proje kuralları AGENTS.md'dedir:

@AGENTS.md

Ayrıntı ve okuma sırası: `README.md` → `docs/AI_WORKFLOW.md` → `recipes/README.md`.

## Seviye
<!-- Bu bölümü proje başında doldurun (seviye tanımları: şablon deposundaki docs/SEVIYE_TANIMLARI.md); doldurulana kadar şablon varsayılanı geçerlidir. -->
Seviye: S2 (N-Tier) · Risk: yok (şablon varsayılanı, henüz görüşme yapılmadı)
Kapsam: <müşterinin asıl ihtiyacı>
İşin ihtiyaçları: e-posta bildirimi · içerik/görsel yükleme · deploy · yedek · spam koruması (her biri: var/yok/çözüm)
Yapmayacaklarımız: self-servis e-posta/hesap silme, soft delete, RowVersion, audit kullanıcı alanları, rol yönetimi (S3) · domain event, API versiyonlama, önbellek (Onion S4-S5)
Seviye atlama: `recipes/` altındaki reçeteyle; sıfırdan yazılmaz.
