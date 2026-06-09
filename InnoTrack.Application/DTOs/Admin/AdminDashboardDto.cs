namespace InnoTrack.Application.DTOs.Admin
{
    public record AdminDashboardDto(
        // ── User Stats ───────────────────────────────────────────────────────────
        int TotalStudents,
        int ActiveStudents,
        int TotalProfessors,
        int ActiveProfessors,
        int NewStudentsThisWeek,
        // ── Team Stats ───────────────────────────────────────────────────────────
        int TotalTeams,
        int TeamsWithoutSupervisor,
        // ── Project Stats ────────────────────────────────────────────────────────
        int TotalProjects,
        int DraftCount,
        int UnderReviewCount,
        int InProgressCount,
        int ApprovedCount,
        int RejectedCount,
        int CompletedCount,
        decimal? AverageOriginalityScore,
        // ── Academic Year ────────────────────────────────────────────────────────
        bool HasActiveAcademicYear,
        string? ActiveAcademicYearName,
        // ── Alerts and Activity ──────────────────────────────────────────────────
        IReadOnlyList<SystemAlertDto> Alerts,
        IReadOnlyList<RecentAuditEntryDto> RecentActivity
    );

    /// <param name="Severity">"Critical" | "Warning" | "Info"</param>
    public record SystemAlertDto(string Severity, string Message, int Count);

    public record RecentAuditEntryDto(
        int Id,
        string ActorName,
        string Action,
        string Details,
        DateTime Timestamp
    );
}