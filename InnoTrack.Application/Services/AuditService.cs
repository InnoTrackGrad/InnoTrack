using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
        public AuditService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task LogActionAsync(int userId, string action, string details)
        {
            var log = new AuditLog { UserId = userId, Action = action, Details = details };
            await _unitOfWork.Repository<AuditLog>().AddAsync(log);
            await _unitOfWork.CompleteAsync();
        }
    }
}
