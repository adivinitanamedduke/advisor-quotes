using Pricing;

namespace API.Middleware.DependencyInjection
{
    public static class PricingModuleServiceCollection
    {
        public static IServiceCollection AddPricingModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<PricingOptions>(configuration.GetSection(PricingOptions.SectionName));

            // Pricing engine session registers IProductCatalog and IPremiumCalculator here

            return services;
        }
    }
}
