using FluentValidation;
using WeeklyPlanner.Core.DTOs;

namespace WeeklyPlanner.API.Validators;

public class UpdateBacklogItemRequestValidator : AbstractValidator<UpdateBacklogItemRequest>
{
    public UpdateBacklogItemRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.")
            .Must(t => !string.IsNullOrWhiteSpace(t)).WithMessage("Title is required.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => x.Description != null)
            .WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.EstimatedEffort)
            .GreaterThan(0).When(x => x.EstimatedEffort.HasValue)
            .WithMessage("Estimated effort must be greater than 0.")
            .LessThanOrEqualTo(999.5m).When(x => x.EstimatedEffort.HasValue)
            .WithMessage("Estimated effort cannot exceed 999.5.")
            .Must(e => e == null || (e.Value * 2) % 1 == 0).When(x => x.EstimatedEffort.HasValue)
            .WithMessage("Estimated effort must be in 0.5 increments.");
    }
}
