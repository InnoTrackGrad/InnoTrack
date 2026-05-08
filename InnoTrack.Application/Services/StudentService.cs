using InnoTrack.Application.DTOs.Students;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Interfaces;

namespace InnoTrack.Application.Services
{
    public class StudentService : IStudentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<StudentProfileDto> GetStudentProfileAsync(int userId)
        {
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(userId);
            if (student == null)
                throw new KeyNotFoundException("Student profile not found.");

            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);

            var hasTeam = await _unitOfWork.Repository<TeamMember>().AnyAsync(tm => tm.StudentId == userId);

            var skills = await GetStudentSkillNamesAsync(userId);

            return new StudentProfileDto(
                student.Id,
                student.FirstName,
                student.LastName,
                student.Email,
                student.DepartmentId,
                department?.Name ?? string.Empty,
                student.GPA,
                student.GraduationYear,
                hasTeam,
                skills
                );
        }

        public async Task<StudentPublicProfileDto> GetPublicStudentProfileAsync(int studentId)
        {
            var student = await _unitOfWork.Repository<Student>().GetByIdAsync(studentId);
            if (student == null)
                throw new KeyNotFoundException("Student not found.");

            var department = await _unitOfWork.Repository<Department>().GetByIdAsync(student.DepartmentId);
            
            var skills = await GetStudentSkillNamesAsync(studentId);

            return new StudentPublicProfileDto(
                student.Id,
                student.FullName,
                department?.Name ?? string.Empty,
                student.GPA,
                student.GraduationYear,
                skills
            );
        }
        private async Task<IReadOnlyList<string>> GetStudentSkillNamesAsync(int studentId)
        {
            var studentSkills = await _unitOfWork.Repository<StudentSkill>()
                .GetAllAsync(ss => ss.StudentId == studentId);

            var skills = new List<string>();
            foreach (var ss in studentSkills)
            {
                var skill = await _unitOfWork.Repository<Skill>().GetByIdAsync(ss.SkillId);
                if (skill != null) skills.Add(skill.Name);
            }
            return skills.AsReadOnly();
        }
    }
}