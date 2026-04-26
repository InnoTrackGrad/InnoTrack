using InnoTrack.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Interfaces
{
    public interface IFileService
    {
        Task<ProjectAttachment> UploadFileAsync(
                    Stream fileStream,
                    string fileName,
                    string contentType,
                    int projectId,
                    int uploaderId);
    }
}
