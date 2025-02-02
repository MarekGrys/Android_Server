using Android_Server.Models;

namespace Android_Server.Services
{
    public interface IAndroidService
    {
        Task UploadPhoto(string photo);
        Task<string> GenerateDocument(string name);
        List<FileDetail> GetFiles();
        Task<PdfFileModel> SendPDF(string fileName);
        Task<string> GetPhotoDescription(string bytes);
        Task<string> GetMultiplePhotosDescription(string[] bytes);
    }
}
