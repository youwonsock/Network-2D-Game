using System.Collections.Generic;

public class CreateAccountPacketReq
{
    public string AccountName;
    public string Password;
}

public class CreateAccountPacketRes
{
    public bool Success;
}

public class LoginAccountPacketReq
{
    public string AccountName;
    public string Password;
}

public class ServerInfo
{
    public string Name;
    public string Ip;
    public int CrowdedLevel;
}

public class LoginAccountPacketRes
{
    public bool Success;
    public List<ServerInfo> ServerList = new List<ServerInfo>();
}
