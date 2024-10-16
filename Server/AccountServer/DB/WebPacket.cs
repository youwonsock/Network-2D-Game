﻿using System.Collections.Generic;

public class CreateAccountPacketReq
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

public class CreateAccountPacketRes
{
    public bool Success { get; set; }
}

public class LoginAccountPacketReq
{
    public string AccountName { get; set; }
    public string Password { get; set; }
}

public class ServerInfo
{
    public string Name { get; set; }
    public string Ip { get; set; }
    public int CrowdedLevel { get; set; }
}

public class LoginAccountPacketRes
{
    public bool Success { get; set; }
    public List<ServerInfo> ServerList { get; set; } = new List<ServerInfo>();
}