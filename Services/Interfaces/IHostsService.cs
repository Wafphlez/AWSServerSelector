namespace AWSServerSelector.Services.Interfaces;

public interface IHostsService
{
    string Read();
    void Write(string content);
    void Backup();
    void FlushDns();
    bool IsHostBlocked(string host);
}
