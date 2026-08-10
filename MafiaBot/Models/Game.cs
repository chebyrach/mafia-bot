using MafiaBot.Enums;
namespace MafiaBot.Models;

public class Game
{
    public List<Player> Players { get; set; }

    public Game(List<string> players)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        else if (players.Count < 3 && players.Count > 15) throw new ArgumentOutOfRangeException(nameof(players));
        foreach (var player in players)
        {
            Player newPlayer = new Player(player, Roles.Civilian);
        }
        GiveRoles();
    }

    private void GiveRoles()
    {
        Players.Shuffle();
        switch (Players.Count)
        {
            case 3:
            {
                Players[0].Role = Roles.Mafia;
                break;
            }
            case >= 4 and <= 5:
            {
                Players[0].Role = Roles.Mafia;
                Players[1].Role = Roles.Detective;
                break;
            }
            case >= 6 and <= 7:
            {
                Players[0].Role = Roles.Mafia;
                Players[1].Role = Roles.Detective;
                Players[2].Role = Roles.Doctor;
                break;
            }
            case >= 8 and <= 11:
            {
                Players[0].Role = Roles.Mafia;
                Players[1].Role = Roles.Detective;
                Players[2].Role = Roles.Doctor;
                Players[3].Role = Roles.Mafia;
                break;
            }
            case >= 12 and <= 15:
            {
                Players[0].Role = Roles.Mafia;
                Players[1].Role = Roles.Detective;
                Players[2].Role = Roles.Doctor;
                Players[3].Role = Roles.Mafia;
                Players[4].Role = Roles.Mafia;
                break;
            }
        }
        Players.Shuffle();
    }
}