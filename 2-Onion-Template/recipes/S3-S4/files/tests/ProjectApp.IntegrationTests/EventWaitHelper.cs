using Microsoft.Extensions.DependencyInjection;
using ProjectApp.Infrastructure.Persistence.Context;

namespace ProjectApp.IntegrationTests;

/// <summary>
/// Olay (in-memory bus) üreten testler için bekleme yardımcısı. Olay handler'ı arka planda, HTTP isteğinden
/// sonra çalışır; testler sabit Task.Delay yerine koşul sağlanana dek yoklar.
/// NOT: Fabrika tek paylaşılan SQLite bağlantısı kullanır; handler ile test aynı anda bağlantıyı kullanırsa
/// "database is locked" (Error 5) oluşabilir. Bu yüzden her yoklamada yeni scope açılır, geçici veritabanı
/// hataları (kilit) koşul sağlanmadı sayılıp yeniden denenir ve yoklamalar kısa aralıklıdır; olay üreten bir
/// isteğin hemen ardından paylaşılan bağlantıda paralel istek atmak yerine önce bu yardımcıyla beklenmelidir.
/// </summary>
public static class EventWaitHelper
{
    public static async Task WaitForAsync(
        this CustomWebApplicationFactory factory,
        Func<AppDbContext, Task<bool>> condition,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));

        while (true)
        {
            try
            {
                using var scope = factory.Services.CreateScope();
                if (await condition(scope.ServiceProvider.GetRequiredService<AppDbContext>()))
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Paylaşılan bağlantıda geçici kilit/çakışma: bir sonraki yoklamada yeniden dene.
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("Beklenen koşul zaman aşımı süresi içinde sağlanmadı.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }
}
