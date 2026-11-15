using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ProjectApp.API.Extensions;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Reverse proxy arkasında gerçek istemci IP'sini (rate limiter için) okur.
    /// ForwardedHeaders:KnownProxies (tek IP) ve ForwardedHeaders:KnownNetworks (CIDR, ör. 10.0.0.0/8) boşsa
    /// yalnızca varsayılan loopback proxy'lere güvenilir.
    /// Production'da ikisi de boşsa uygulama açılır, proxy yok kabul edilir ve açılışta tek bir uyarı loglanır;
    /// yük dengeleyici arkasında tüm istemciler proxy IP'siyle görünür ve tek bir rate limit bölümünü paylaşır.
    /// Proxy'siz (doğrudan internete açık) dağıtımda ForwardedHeaders:NoProxy = true verilirse uyarı loglanmaz.
    /// </summary>
    public static IServiceCollection AddForwardedHeadersSetup(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
        var noProxy = configuration.GetValue<bool>("ForwardedHeaders:NoProxy");

        var proxies = knownProxies.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        var networks = knownNetworks.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();

        if (environment.IsProduction() && !noProxy && proxies.Length == 0 && networks.Length == 0)
        {
            // Seçenekler middleware kurulurken (açılışta) bir kez oluşturulur; uyarı da bir kez yazılır.
            services.AddOptions<ForwardedHeadersOptions>().Configure<ILoggerFactory>((_, loggerFactory) =>
                loggerFactory.CreateLogger(typeof(ForwardedHeadersExtensions)).LogWarning(
                    "ForwardedHeaders:KnownProxies ve ForwardedHeaders:KnownNetworks boş; reverse proxy yok kabul edildi. " +
                    "Proxy arkasındaysanız bu ayarları verin, değilseniz ForwardedHeaders:NoProxy = true ile bu uyarıyı kapatın."));
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var proxy in proxies)
            {
                if (!IPAddress.TryParse(proxy, out var address))
                {
                    throw new InvalidOperationException($"ForwardedHeaders:KnownProxies içinde geçersiz IP adresi: '{proxy}'.");
                }

                options.KnownProxies.Add(address);
            }

            foreach (var network in networks)
            {
                if (!System.Net.IPNetwork.TryParse(network, out var ipNetwork))
                {
                    throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks içinde geçersiz CIDR: '{network}'.");
                }

                options.KnownIPNetworks.Add(ipNetwork);
            }
        });

        return services;
    }
}
