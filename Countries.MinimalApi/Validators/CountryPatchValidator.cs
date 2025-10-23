using System.Text.RegularExpressions;
using Countries.MinimalApi.Models;
using FluentValidation;
using FluentValidation.Results;

namespace Countries.MinimalApi.Validators;

public class CountryPatchValidator : AbstractValidator<CountryPatch>
{
    public CountryPatchValidator()
    {
        RuleFor(x => x.Description).NotEmpty().WithMessage("{ParameterName} cannot be empty")
            .Custom((name, context) =>
            {
                var rg = new Regex("<.*?>"); // try to match HTML tags
                if (rg.Matches(name).Count > 0)
                    // Raises an error
                    context.AddFailure(new ValidationFailure("Description", "The description has invalid content"));
            });
    }
}