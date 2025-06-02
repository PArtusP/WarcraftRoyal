using System;


public class ConnectionString
{
    public Guid Id;
    public string Name;
    public bool Licensed;
    public int CharacterId;
}
public class PlayerSettings
{
    static public string Name { get; set; }
    static public int CharacterId { get; set; }
}
