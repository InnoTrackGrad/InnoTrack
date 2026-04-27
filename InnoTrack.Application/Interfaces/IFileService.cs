using InnoTrack.Application.DTOs.Projects;
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
        Task<ProjectAttachmentDto> UploadFileAsync(
                    Stream fileStream,
                    string fileName,
                    string contentType,
                    long fileSize,
                    int projectId,
                    int uploaderId);

        Task<ProjectAttachment> GetAttachmentIfAuthorizedAsync(int attachmentId, int userId);
    }
}
