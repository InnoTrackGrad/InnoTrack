using InnoTrack.API.Attributes;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
//using System.Reflection.Metadata;
using System.Security.Claims;

namespace InnoTrack.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeRoles(UserRole.Student)]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;
        private readonly IProjectAnalysisService _projectAnalysisService;
        private readonly IUnitOfWork _unitOfWork;

        public ProjectsController(IProjectService projectService, IFileService fileService, IProjectAnalysisService projectAnalysisService, IUnitOfWork unitOfWork)
        {
            _projectService = projectService;
            _fileService = fileService;
            _projectAnalysisService = projectAnalysisService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Uploads a project attachment file for the specified project.
        /// </summary>
        /// <param name="projectId">
        /// The identifier of the target project.
        /// </param>
        /// <param name="file">
        /// The file to upload.
        /// </param>
        /// <returns>
        /// Returns the uploaded attachment metadata including file identifier and access path.
        /// </returns>
        /// <remarks>
        /// Supported file types include documents, images, and compressed archives.
        /// File uploads are limited to 25 MB.
        /// </remarks>
        [HttpPost("{projectId}/upload")]
        public async Task<IActionResult> UploadAttachment(int projectId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file was uploaded.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            using var stream = file.OpenReadStream();
            var attachment = await _fileService.UploadFileAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                projectId,
                userId
            );

            return Ok(attachment);
        }

        /// <summary>
        /// Submits a project proposal for AI originality analysis and professor review.
        /// </summary>
        /// <param name="projectId">
        /// The identifier of the project to submit.
        /// </param>
        /// <param name="dto">
        /// The submission request containing supervisor assignment
        /// and proposal-related metadata.
        /// </param>
        /// <returns>
        /// Returns a confirmation message after successful submission.
        /// </returns>
        /// <remarks>
        /// Only team leaders may submit projects.
        /// The submission automatically triggers asynchronous AI originality analysis.
        /// </remarks>
        [HttpPost("{projectId}/submit")]
        public async Task<IActionResult> SubmitProject(int projectId, [FromBody] SubmitProjectRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _projectService.VerifyProjectForSubmissionAsync(projectId, userId, dto);
            return Ok(new { message = "Project submitted successfully. AI is generating the originality report." });
        }

        /// <summary>
        /// Generates and downloads the AI originality analysis report as a PDF document.
        /// </summary>
        /// <param name="projectId">
        /// The identifier of the analyzed project.
        /// </param>
        /// <returns>
        /// Returns a downloadable PDF containing originality scores,
        /// AI analysis summary, and similarity breakdown.
        /// </returns>
        /// <remarks>
        /// The report is dynamically generated using QuestPDF
        /// based on the stored AI analysis results.
        /// </remarks>
        [HttpGet("{projectId}/originality-report/pdf")]
        public async Task<IActionResult> DownloadReportPdf(int projectId)
        {
            var report = await _projectAnalysisService.GetOriginalityReportAsync(projectId);

            var pdfDocument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Text("InnoTrack AI Originality Report").SemiBold().FontSize(20);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Project ID: {report.ProjectId}");
                        col.Item().Text($"Originality Score: {report.OverallScore}%").FontColor(report.OverallScore < 60 ? Colors.Red.Medium : Colors.Green.Medium);
                        col.Item().Text($"AI Summary: {report.Summary}");
                        if (report.SimilarProjects != null && report.SimilarProjects.Any())
                        {
                            col.Item().PaddingTop(15).Text("Similar Projects Breakdown:").SemiBold().FontSize(14);

                            col.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().BorderBottom(1).Padding(2).Text("Project (ID - Title)").SemiBold();
                                    header.Cell().BorderBottom(1).Padding(2).Text("Match Reason").SemiBold();
                                    header.Cell().BorderBottom(1).Padding(2).Text("Similarity %").SemiBold();
                                });

                                foreach (var sp in report.SimilarProjects)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                        .Text($"[{sp.Id?.ToString() ?? "?"}] {sp.Title}");

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                        .Text(sp.MatchReason ?? "No reason provided");

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(2)
                                        .Text($"{sp.Similarity}%").FontColor(sp.Similarity > 40 ? Colors.Red.Medium : Colors.Black);
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(15).Text("No highly similar projects found. Great originality!")
                                .Italic().FontColor(Colors.Green.Medium);
                        }
                    });
                });
            });

            byte[] pdfBytes = pdfDocument.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"OriginalityReport_PRJ{projectId}.pdf");
        }
    }
}
