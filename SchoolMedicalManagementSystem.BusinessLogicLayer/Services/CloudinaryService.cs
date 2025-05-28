using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolMedicalManagementSystem.BusinessLogicLayer.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            Account account = new Account
            {
                Cloud = cloudName,
                ApiKey = apiKey,
                ApiSecret = apiSecret
            };

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(byte[] imageData, string fileName)
        {
            using var ms = new MemoryStream(imageData);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, ms),
                PublicId = $"{DateTime.UtcNow.Ticks}_{fileName}"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.AbsoluteUri;
        }
    }
}
