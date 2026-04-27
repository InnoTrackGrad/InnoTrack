using FluentValidation;
using InnoTrack.Application.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Validators
{
    public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
    {
        public CreateTeamDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Team name is required.")
                .MaximumLength(100).WithMessage("Team name cannot exceed 100 characters.");

            RuleFor(x => x.MaxSize)
                .InclusiveBetween(2, 11)
                .WithMessage("Team size must be between 2 and 11 members according to faculty regulations.");
        }
    }
}
