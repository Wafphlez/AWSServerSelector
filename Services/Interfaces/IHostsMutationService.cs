namespace AWSServerSelector.Services.Interfaces;

public interface IHostsMutationService
{
    string Read();
    void Write(string content);
    void Backup();
    void FlushDns();
    bool IsHostBlocked(string host);
    string ReadDefaultTemplate();
}
