using FluentValidation;
using InnoTrack.Application.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Validators
{
    public class JoinRequestValidator : AbstractValidator<JoinRequestDto>
    {
        public JoinRequestValidator()
        {
            RuleFor(x => x.JoinCode)
                .NotEmpty().WithMessage("Join code is required.")
                .Length(8).WithMessage("Invalid join code format.");
        }
    }
}
