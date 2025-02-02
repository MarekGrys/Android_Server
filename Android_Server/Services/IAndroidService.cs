using Android_Server.Models;

namespace Android_Server.Services
{
    public interface IAndroidService
    {
        Task UploadPhoto(string photo);
        string GenerateDocument(string name);
        List<FileDetail> GetFiles();
        Task<PdfFileModel> SendPDF(string fileName);
    }
}
