using MafiaBot.Enums;

namespace MafiaBot.Models;

public class Player
{
    public string Tag { get; set; }
    public Roles Role { get; set; }
    public bool IsAlive { get; set; } = true;

    public Player(string tag, Roles role)
    {
        Tag = tag;
        Role = role;
    }

    public bool CheckForMafia()
    {
        if (this.Role == Roles.Mafia) return true;
        return false;
    }
    
    public bool CheckForDetective()
    {
        if (this.Role == Roles.Detective) return true;
        return false;
    }
    
    public bool CheckForDoctor()
    {
        if (this.Role == Roles.Doctor) return true;
        return false;
    }
    
    public void Kill()
    {
        IsAlive = false;
    }

    public bool CheckForAlive()
    {
        if (IsAlive) return true;
        return false;
    }
    
    public bool Equals(Player? obj)
    {
        if(obj == null) return false;
        return this.Tag.Equals(obj.Tag);
    }
}