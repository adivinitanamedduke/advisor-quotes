using Business.Abstractions.Config;
using Business.Abstractions.Interfaces.Business.Quotes;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Services.Quotes;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;
using Data.SQL.Repositories.Quotes;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<IQuoteRepository, QuoteRepository>();
            services.AddScoped<IQuoteService, QuoteService>();

            return services;
        }
    }
}
