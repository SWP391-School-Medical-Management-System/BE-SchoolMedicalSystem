using Microsoft.AspNetCore.Http;
using SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Models.Requests.UserRequest
{
    public class UpdateUserProfileRequest : BaseUserUpdateRequest
    {
        public IFormFile? ProfileImage { get; set; }
    }
}
