using Microsoft.Extensions.Options;
using Pricing;

namespace API.Middleware.DependencyInjection
{
    public static class PricingModuleServiceCollection
    {
        public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration configuration)
        {
            // A broken catalogue stops the API at startup rather than failing the first quote
            services.AddOptions<PricingOptions>()
                .Bind(configuration.GetSection(PricingOptions.SectionName))
                .ValidateOnStart();
            services.AddSingleton<IValidateOptions<PricingOptions>, PricingOptionsValidator>();

            // Both are stateless and read options once, so one instance serves every request
            services.AddSingleton<IProductCatalog, ProductCatalog>();
            services.AddSingleton<IPremiumCalculator, PremiumCalculator>();

            return services;
        }
    }
}
