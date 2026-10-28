using Business.Abstractions.Exceptions;
using FluentValidation;

namespace Business.Validators
{
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Runs the validator and throws BadRequestException (400) with every failure, messages joined by "; ".
        /// Call it as the first line of a service method, before any repository or pricing work.
        /// </summary>
        public static async Task ValidateOrThrowAsync<T>(
            this IValidator<T> validator,
            T? request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new BadRequestException("Request body is required");
            }

            var result = await validator.ValidateAsync(request, cancellationToken);
            if (result.IsValid)
            {
                return;
            }

            var errors = result.Errors
                .Select(e => new ApiError(e.PropertyName, e.ErrorMessage))
                .ToList();

            throw new BadRequestException(string.Join("; ", errors.Select(e => e.Message)), errors);
        }
    }
}
