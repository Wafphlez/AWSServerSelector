namespace AWSServerSelector.Services.Interfaces;

public interface IExternalNavigationService
{
    void OpenUrl(string url);
    void OpenFile(string filePath);
    void OpenFolder(string folderPath);
}
