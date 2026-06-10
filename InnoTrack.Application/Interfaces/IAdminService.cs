using InnoTrack.Application.Common;
using InnoTrack.Application.DTOs.Admin;
using InnoTrack.Domain.Entities.Enums;

namespace InnoTrack.Application.Interfaces
{
    public interface IAdminService
    {
        // ── Dashboard ────────────────────────────────────────────────────────────
        Task<AdminDashboardDto> GetDashboardAsync();

        // ── Student Management ───────────────────────────────────────────────────
        Task<PagedResult<StudentAdminViewDto>> SearchStudentsAsync(
            string? search, int? departmentId, bool? hasTeam, bool? isActive,
            int pageNumber, int pageSize);

        Task<StudentDetailForAdminDto> GetStudentDetailAsync(int studentId);
        Task SetStudentActiveStatusAsync(int studentId, bool isActive);
        Task ResetStudentPasswordAsync(int studentId, string newPassword);

        /// <summary>
        /// Soft-deletes a student account (IsDeleted = true).
        /// Blocked if the student is a team leader with an active project.
        /// </summary>
        Task SoftDeleteStudentAsync(int adminId, int studentId);

        // ── Team Management ──────────────────────────────────────────────────────
        Task<PagedResult<AdminTeamListItemDto>> GetAllTeamsAsync(string? search, int pageNumber, int pageSize);
        Task AssignSupervisorToTeamAsync(int teamId, int professorId);
        Task DeleteTeamByAdminAsync(int adminId, int teamId);
        Task RemoveSupervisorFromTeamAsync(int teamId);

        // ── Project Management ───────────────────────────────────────────────────
        Task<PagedResult<AdminProjectListItemDto>> GetAllProjectsAsync(
            string? status, int? academicYearId, int pageNumber, int pageSize);

        Task<AdminProjectDetailDto> GetProjectDetailAsync(int projectId);

        /// <summary>Force-transitions a project to any status with a mandatory audit reason.</summary>
        Task OverrideProjectStatusAsync(
            int adminId, int projectId, ProjectStatus newStatus, string reason);

        /// <summary>Reassigns the supervising professor for a project's team.</summary>
        Task ReassignProjectSupervisorAsync(int adminId, int projectId, int newProfessorId);

        /// <summary>
        /// Finds projects stuck in UnderReview for over 48 hours with no originality score
        /// (indicating a failed Hangfire job) and resets them to Draft so students can resubmit.
        /// </summary>
        Task<int> ResetStuckProjectsAsync(int adminId);

        // ── Audit Logs ───────────────────────────────────────────────────────────
        Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
            int? userId, string? action, DateTime? from, DateTime? to,
            int pageNumber, int pageSize);
    }
}