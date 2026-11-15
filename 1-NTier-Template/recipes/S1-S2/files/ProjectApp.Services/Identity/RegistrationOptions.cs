namespace ProjectApp.Services.Identity;

/// <summary>
/// Kayıt (register) ayarı. <c>Registration:AllowPublic</c> true ise herkes kayıt olur ve User rolü alır;
/// false (varsayılan) ise /auth/register yalnızca Admin'e açıktır (kapalı sistemler: okul, yurt, kurum içi).
/// Uygulama başlarken bir kez okunur; değiştirdikten sonra uygulamayı yeniden başlatın.
/// </summary>
public class RegistrationOptions
{
    public const string SectionName = "Registration";

    public bool AllowPublic { get; set; }   // varsayılan false: yalnızca Admin
}
