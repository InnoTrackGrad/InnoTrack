using AutoMapper;
using InnoTrack.Application.DTOs.Projects;
using InnoTrack.Application.Interfaces;
using InnoTrack.Domain.Entities;
using InnoTrack.Domain.Entities.Enums;
using InnoTrack.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoTrack.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProjectService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ProjectResponseDto> CreateProjectAsync(int leaderId, CreateProjectDto dto)
        {
            var leaderRecord = await _unitOfWork.Repository<TeamMember>().FindAsync(tm => tm.StudentId == leaderId && tm.Role == TeamMemberRole.Leader);
            if (leaderRecord == null)
                throw new UnauthorizedAccessException("Only the team leader can submit a project proposal.");

            var existingProject = await _unitOfWork.Repository<Project>().FindAsync(p => p.TeamId == leaderRecord.TeamId);
            if (existingProject != null)
                throw new InvalidOperationException("Your team already has a submitted project.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var project = new Project
                {
                    Title = dto.Title,
                    Abstract = dto.Abstract,
                    Description = dto.Description,
                    DomainId = dto.DomainId,
                    TeamId = leaderRecord.TeamId,
                    Status = ProjectStatus.Draft
                };

                await _unitOfWork.Repository<Project>().AddAsync(project);
                await _unitOfWork.CompleteAsync();

                foreach (var techId in dto.TechnologyIds)
                {
                    var projectTech = new ProjectTechnology
                    {
                        ProjectId = project.Id,
                        TechnologyId = techId
                    };
                    await _unitOfWork.Repository<ProjectTechnology>().AddAsync(projectTech);
                }
                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ProjectResponseDto(project.Id, project.Title, project.Status);
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
