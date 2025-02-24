using FluentValidation;
using VolleyList.Models;

namespace VolleyList.Validators;

public class ListingValidator : AbstractValidator<Listing>
{
    public ListingValidator()
    {
        RuleFor(listing => listing.Name)
            .NotNull().NotEmpty()
            .WithState(_ => new CustomValidationState { NameErrorMessage = "Nome não pode ser vazio" });

        RuleFor(listing => listing.MaxSize)
            .Must(size => size > 1).When(listing => listing.MaxSize is not null)
            .WithState(_ => new CustomValidationState { SizeErrorMessage = "Tamanho deve ser maior do que 1" });

        RuleFor(listing => listing.LimitDateToRemoveNameAndNotPay)
            .Must(limitDate => limitDate > DateTime.UtcNow).When(listing => listing.LimitDateToRemoveNameAndNotPay is not null)
            .WithState(_ => new CustomValidationState { DateErrorMessage = "Data limite não pode ser no passado" });
    }
}

public record CustomValidationState
{
    public string? NameErrorMessage { get; init; }
    public string? SizeErrorMessage { get; init; }
    public string? DateErrorMessage { get; init; }
}