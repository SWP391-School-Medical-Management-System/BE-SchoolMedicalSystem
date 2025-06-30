using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.VaccineSessionRequest;
using SchoolMedicalManagementSystem.DataAccessLayer.Entities;
using SchoolMedicalManagementSystem.DataAccessLayer.UnitOfWorks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Validators.VaccineSession
{
    public class MarkStudentVaccinatedRequestValidator : AbstractValidator<MarkStudentVaccinatedRequest>
    {
        private readonly IUnitOfWork _unitOfWork;

        public MarkStudentVaccinatedRequestValidator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

            RuleFor(x => x.StudentId)
                .NotEmpty().WithMessage("ID học sinh không được để trống.")
                .MustAsync(BeValidStudentAsync).WithMessage("Học sinh không tồn tại hoặc không có vai trò STUDENT.");

            RuleFor(x => x.Symptoms)
                .MaximumLength(500).WithMessage("Triệu chứng không được vượt quá 500 ký tự.");

            RuleFor(x => x.NoteAfterSession)
                .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.");
        }

        private async Task<bool> BeValidStudentAsync(Guid studentId, CancellationToken cancellationToken)
        {
            var student = await _unitOfWork.GetRepositoryByEntity<ApplicationUser>().GetQueryable()
                .FirstOrDefaultAsync(u => u.Id == studentId && u.UserRoles.Any(ur => ur.Role.Name == "STUDENT") && !u.IsDeleted, cancellationToken);
            return student != null;
        }
    }
}
