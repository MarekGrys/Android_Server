namespace Android_Server.Services
{
    public interface IAndroidService
    {
        Task UploadPhoto(string photo);
        string GenerateDocument();
    }
}
