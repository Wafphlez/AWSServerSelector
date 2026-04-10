namespace AWSServerSelector.Models;

public enum ApplyMode
{
    Gatekeep,
    UniversalRedirect
}

public enum BlockMode
{
    Both,
    OnlyPing,
    OnlyService
}
