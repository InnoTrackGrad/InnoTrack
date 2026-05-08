namespace InnoTrack.Application.DTOs.Students
{
    public record StudentPublicProfileDto(
        int Id,
        string FullName,
        string DepartmentName,
        decimal? GPA,
        int GraduationYear,
        IReadOnlyList<string> Skills
    );
}