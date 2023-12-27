using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System;
using System.Net.Http;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет HttpClient с прокси, если это задано в настройках.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddHttpClientWithProxy(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var conf = serviceProvider.GetService<ServiceConfiguration>();
            services.AddHttpContextAccessor();
            services.AddTransient<MfTraceIdHeaderSetter>();

            services.AddHttpClient(nameof(MultiFactorApiClient), client =>
            {
                client.Timeout = conf.ApiTimeout;
            })
            .ConfigurePrimaryHttpMessageHandler(prov =>
            {
                var handler = new HttpClientHandler();

                if (string.IsNullOrWhiteSpace(conf.ApiProxy)) return handler;
                logger.Debug("Using proxy " + conf.ApiProxy);
                if (!WebProxyFactory.TryCreateWebProxy(conf.ApiProxy, out var webProxy))
                {
                    throw new Exception("Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
                }
                handler.Proxy = webProxy;

                return handler;
            })
            .AddHttpMessageHandler<MfTraceIdHeaderSetter>();
        }
    }
}
