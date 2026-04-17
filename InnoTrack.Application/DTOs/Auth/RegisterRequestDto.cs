using InnoTrack.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.DTOs.Auth
{
    public record RegisterRequestDto
        (string FirstName, string LastName, string Email, string Password, int DepartmentId);
}
