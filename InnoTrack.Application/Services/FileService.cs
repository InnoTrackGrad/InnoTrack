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
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProjectAttachment> UploadFileAsync(
            Stream fileStream, string fileName, string contentType, int projectId, int uploaderId)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            var attachment = new ProjectAttachment
            {
                FileName = uniqueFileName,
                OriginalName = fileName,
                ContentType = contentType,
                FilePath = $"/uploads/{uniqueFileName}",
                ProjectId = projectId,
                UploaderId = uploaderId,
                UploadDate = DateTime.UtcNow,
                FileSize = fileStream.Length
            };

            await _unitOfWork.Repository<ProjectAttachment>().AddAsync(attachment);
            await _unitOfWork.CompleteAsync();
            return attachment;
        }
    }
}
