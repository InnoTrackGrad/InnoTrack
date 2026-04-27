using InnoTrack.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _env;

        public FilesController(IFileService fileService, IWebHostEnvironment env)
        {
            _fileService = fileService;
            _env = env;
        }
        [HttpGet("{attachmentId:int}")]
        public async Task<IActionResult> Download(int attachmentId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var attachment = await _fileService.GetAttachmentIfAuthorizedAsync(attachmentId, userId);
            var fullPath = Path.Combine(_env.ContentRootPath, "private-uploads", attachment.FileName);
            
            if (!System.IO.File.Exists(fullPath))
                return NotFound("The physical file was not found on the server.");

            return PhysicalFile(fullPath, attachment.ContentType ?? "application/octet-stream", attachment.OriginalName);
        }
    }
}
