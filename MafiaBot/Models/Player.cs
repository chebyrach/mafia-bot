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
    
    public void Kill()
    {
        IsAlive = false;
    }
}