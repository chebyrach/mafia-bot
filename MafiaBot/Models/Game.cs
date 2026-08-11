using MafiaBot.Enums;
namespace MafiaBot.Models;

public class Game
{
    private Player? _mafiaTarget;
    private Player? _doctorTarget;
    private List<Player> _players = new List<Player>();

    public Game(List<string> players)
    {
        if (_players.Count < 3 && _players.Count > 15) throw new ArgumentOutOfRangeException(nameof(_players));
        foreach (var player in players)
        {
            Player newPlayer = new Player(player, Roles.Civilian);
            _players.Add(newPlayer);
        }
        GiveRoles();
    }

    private void GiveRoles()
    {
        _players = _players.OrderBy(_ => Random.Shared.Next()).ToList();
        switch (_players.Count)
        {
            case 3:
            {
                _players[0].Role = Roles.Mafia;
                break;
            }
            case >= 4 and <= 5:
            {
                _players[0].Role = Roles.Mafia;
                _players[1].Role = Roles.Detective;
                break;
            }
            case >= 6 and <= 7:
            {
                _players[0].Role = Roles.Mafia;
                _players[1].Role = Roles.Detective;
                _players[2].Role = Roles.Doctor;
                break;
            }
            case >= 8 and <= 11:
            {
                _players[0].Role = Roles.Mafia;
                _players[1].Role = Roles.Detective;
                _players[2].Role = Roles.Doctor;
                _players[3].Role = Roles.Mafia;
                break;
            }
            case >= 12 and <= 15:
            {
                _players[0].Role = Roles.Mafia;
                _players[1].Role = Roles.Detective;
                _players[2].Role = Roles.Doctor;
                _players[3].Role = Roles.Mafia;
                _players[4].Role = Roles.Mafia;
                break;
            }
        }
        _players = _players.OrderBy(_ => Random.Shared.Next()).ToList();
    }

    public List<string>? GetMafia()
    {
        var mafia = _players.Where(x => x.CheckForMafia() && x.CheckForAlive()).Select(x => x.Tag).ToList();
        if(mafia.Count == 0) return null;
        return mafia;
    }
    
    public string? GetDetective()
    {
        return _players.FirstOrDefault(x => x.CheckForDetective() && x.CheckForAlive())?.Tag;
    }
    
    public string? GetDoctor()
    {
        return _players.FirstOrDefault(x => x.CheckForDoctor() && x.CheckForAlive())?.Tag;
    }

    public void MafiaWalks(string targetId)
    {
        var targetObj = _players.First(x => x.Tag == targetId);
        if(targetObj == null) throw new NullReferenceException();
        _mafiaTarget = targetObj;
    }
 

    public bool DetectiveWalks(string targetId)
    {
        var targetObj = _players.First(x => x.Tag == targetId);
        if(targetObj == null) throw new NullReferenceException();
        return targetObj.CheckForMafia();
    }
    
    public void DoctorWalks(string targetId)
    {
        var targetObj = _players.First(x => x.Tag == targetId);
        if(targetObj == null) throw new NullReferenceException();
        _doctorTarget = targetObj;
    }

    public string? CheckRoundResults()
    {
        if(_mafiaTarget == null) throw new NullReferenceException();
        if(_players.FirstOrDefault(x => x.CheckForDoctor() && x.CheckForAlive()) != null)
        {
            if (_mafiaTarget.Equals(_doctorTarget))
            {
                return null;
            }
        }
        _players.First(x => x.Equals(_mafiaTarget)).Kill();
        return _mafiaTarget.Tag;
    }

    public bool CheckForCivilianWin()
    {
        return _players.FirstOrDefault(x => x.CheckForMafia() && x.CheckForAlive()) == null;
    }
    
    public bool CheckForMafiaWin()
    {
        int civilians =  _players.Where(x => x.CheckForAlive() && !x.CheckForMafia()).Count();
        int mafia =  _players.Where(x => x.CheckForAlive() && x.CheckForMafia()).Count();
        return mafia >= civilians;
    }
}