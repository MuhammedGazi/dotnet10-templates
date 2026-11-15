namespace ProjectApp.Application.Contracts.Identity;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    /// <summary>[Hazır altyapı] JWT'deki kullanıcı adı (loglama, denetim izi, bildirim metinleri için).</summary>
    string? UserName { get; }
    bool IsAuthenticated { get; }

    /// <summary>
    /// İstek sahibinin JWT'sinde verilen rol var mı (rol bazlı veri kapsamı için). Anonim istekte false döner.
    /// Roller token'dan okunur; rol değişikliği security stamp'i yenilediği için eski token zaten geçersizdir.
    /// </summary>
    bool IsInRole(string role);
}
