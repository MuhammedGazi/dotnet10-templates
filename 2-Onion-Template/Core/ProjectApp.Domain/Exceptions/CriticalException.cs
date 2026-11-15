namespace ProjectApp.Domain.Exceptions;

/// <summary>
/// [Hazır altyapı] Acil müdahale gerektiren hatalar için fırlatılır (ör. ödeme sağlayıcısı tutarsızlığı).
/// <c>CriticalExceptionHandler</c> bunu Critical seviyesinde loglar; log altyapınızda bu seviyeye alarm bağlayın.
/// Örnek: <c>throw new CriticalException("Ödeme tutarı ile sipariş tutarı uyuşmuyor.");</c>
/// </summary>
public class CriticalException(string message) : Exception(message);
