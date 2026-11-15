namespace ProjectApp.Repositories;

/// <summary>
/// İstek sahibi kullanıcının bilgilerini veri erişim katmanına taşır.
/// Uygulaması (HttpContext tabanlı) ProjectApp.API katmanında yapılır.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    /// <summary>[Hazır altyapı] JWT'deki kullanıcı adı (loglama, denetim izi, bildirim metinleri için).</summary>
    string? UserName { get; }

    bool IsAuthenticated { get; }
}
