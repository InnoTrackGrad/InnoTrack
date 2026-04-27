using FluentValidation;
using InnoTrack.Application.DTOs.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Validators
{
    public class HandleRequestValidator : AbstractValidator<HandleRequestDto>
    {
        public HandleRequestValidator()
        {
            RuleFor(x => x.RequestId)
                .GreaterThan(0).WithMessage("A valid request ID is required.");
        }
    }

}
