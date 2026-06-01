using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Interfaces;

namespace InnoTrack.Application.Services
{
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".txt", ".png", ".jpg", ".jpeg", ".zip"
        };
        private static readonly long MaxFileSizeBytes = 25 * 1024 * 1024;
        public async Task<ProjectAttachmentDto> UploadFileAsync(
            Stream fileStream, string fileName, string contentType, long fileSize, int projectId, int uploaderId)
        {
            var safeFileName = Path.GetFileName(fileName);
            var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"File type '{extension}' is not allowed.");

            if (fileSize > MaxFileSizeBytes)
                throw new ArgumentException("File exceeds the maximum allowed size of 25 MB.");

            var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "private-uploads");
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
                FilePath = $"/api/files/{uniqueFileName}",
                ProjectId = projectId,
                UploaderId = uploaderId,
                UploadDate = DateTime.UtcNow,
                FileSize = fileSize
            };

            await _unitOfWork.Repository<ProjectAttachment>().AddAsync(attachment);
            await _unitOfWork.CompleteAsync();

            return new ProjectAttachmentDto(attachment.Id, attachment.OriginalName, attachment.FilePath);
        }

        public async Task<ProjectAttachment> GetAttachmentIfAuthorizedAsync(int attachmentId, int userId)
        {
            var attachment = await _unitOfWork.Repository<ProjectAttachment>().GetByIdAsync(attachmentId);
            if (attachment == null)
                throw new KeyNotFoundException("Attachment not found.");

            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(attachment.ProjectId);
            if (project == null) throw new KeyNotFoundException("Project not found.");

            var isMember = await _unitOfWork.Repository<TeamMember>()
                .FindAsync(tm => tm.TeamId == project.TeamId && tm.StudentId == userId);
            if (isMember == null)
                throw new UnauthorizedAccessException("You are not authorized to download this file. Only team members can access it.");

            return attachment;
        }
    }
}
