namespace AWSServerSelector.Services.Interfaces;

public interface IHostsFileService
{
    string Read();
    string ReadDefaultTemplate();
    void Write(string content);
    void Backup();
    void FlushDns();
    bool IsHostBlocked(string host);
}
