using Pricing;

namespace API.Middleware.DependencyInjection
{
    public static class PricingModuleServiceCollection
    {
        public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PricingOptions>(configuration.GetSection(PricingOptions.SectionName));

            // Both are stateless and read options once, so one instance serves every request
            services.AddSingleton<IProductCatalog, ProductCatalog>();
            services.AddSingleton<IPremiumCalculator, PremiumCalculator>();

            return services;
        }
    }
}
