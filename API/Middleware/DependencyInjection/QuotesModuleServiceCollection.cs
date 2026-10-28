using Business.Abstractions.Config;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;
using FluentValidation;

namespace API.Middleware.DependencyInjection
{
    public static class QuotesModuleServiceCollection
    {
        public static IServiceCollection AddQuotesModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<IdempotencyOptions>(configuration.GetSection(IdempotencyOptions.SectionName));

            // Validators are registered explicitly (no assembly scan) and injected into the service
            services.AddScoped<IValidator<CreateQuoteRequest>, CreateQuoteRequestValidator>();
            services.AddScoped<IValidator<SearchQuotesRequest>, SearchQuotesRequestValidator>();
            services.AddScoped<IValidator<UpdateQuoteStatusRequest>, UpdateQuoteStatusRequestValidator>();

            // Quote lifecycle session registers IQuoteService and IQuoteRepository here

            return services;
        }
    }
}
