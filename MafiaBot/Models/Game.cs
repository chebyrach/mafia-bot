using MafiaBot.Enums;
namespace MafiaBot.Models;

public class Game
{
    private Player mafiaTarget;
    private Player doctorTarget;
    public List<Player> Players { get; set; }

    public Game(List<string> players)
    {
        if (players == null) throw new ArgumentNullException(nameof(players));
        else if (players.Count < 3 && players.Count > 15) throw new ArgumentOutOfRangeException(nameof(players));
        foreach (var player in players)
        {
            Player newPlayer = new Player(player, Roles.Civilian);
            Players.Add(newPlayer);
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

    public void MafiaWalks(string targetId)
    {
        var targetObj = Players.First(x => x.Tag == targetId);
        if(targetObj != null) throw new NullReferenceException();
        mafiaTarget = targetObj;
    }
    
    public bool isDetectiveAlive()
    {
        var doctorObj = Players.First(x => x.CheckForDetective());
        return doctorObj.IsAlive;
    }

    public bool DetectiveWalks(string targetId)
    {
        var targetObj = Players.First(x => x.Tag == targetId);
        if(targetObj != null) throw new NullReferenceException();
        return targetObj.CheckForMafia();
    }
    
    public bool isDoctorAlive()
    {
        var doctorObj = Players.First(x => x.CheckForDoctor());
        return doctorObj.IsAlive;
    }
    
    public void DoctorWalks(string targetId)
    {
        var targetObj = Players.First(x => x.Tag == targetId);
        if(targetObj != null) throw new NullReferenceException();
        doctorTarget = targetObj;
    }

    public string? CheckRoundResults()
    {
        if (mafiaTarget.Equals(doctorTarget))
        {
            return null;
        }
        Players.First(x => x.Equals(mafiaTarget)).Kill();
        return mafiaTarget.Tag;
    }

    public bool CheckForCivilianWin()
    {
        return Players.First(x => x.CheckForMafia() && x.CheckforAlive() ) == null;
    }
    
    public bool CheckForMafiaWin()
    {
        int civilians =  Players.Where(x => !x.CheckForMafia()).Count();
        int mafias =  Players.Where(x => x.CheckForMafia()).Count();
        return civilians >= mafias;
    }
}