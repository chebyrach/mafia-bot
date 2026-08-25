using MafiaBot.Enums;
namespace MafiaBot.Models;

public class Game
{
    private Player? _mafiaTarget;
    private Player? _doctorTarget;
    private List<Player> _players = new List<Player>();

    public Game(List<long> players)
    {   
        if (players.Count < 3 && players.Count > 15) throw new ArgumentOutOfRangeException(nameof(players));
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
    
    public List<long> GetAllAlive()
    {
        return _players.Where(x => x.CheckForAlive()).Select(x => x.Id).ToList();
    }

    public List<long> GetListForMafia()
    {
        return _players.Where(x => x.CheckForAlive() && !x.CheckForMafia()).Select(x => x.Id).ToList();
    }

    public List<long> GetListForDetective()
    {
        return _players.Where(x => x.CheckForAlive() && !x.CheckForDetective()).Select(x => x.Id).ToList();
    }
    
    public List<long> GetListForDoctor()
    {
        return _players.Where(x => x.CheckForAlive() && x != _doctorTarget).Select(x => x.Id).ToList();
    }

    public List<long>? GetMafia()
    {
        var mafia = _players.Where(x => x.CheckForMafia() && x.CheckForAlive()).Select(x => x.Id).ToList();
        if(mafia.Count == 0) return null;
        return mafia;
    }
    
    public long? GetDetective()
    {
        return _players.FirstOrDefault(x => x.CheckForDetective() && x.CheckForAlive())?.Id;
    }
    
    public long? GetDoctor()
    {
        return _players.FirstOrDefault(x => x.CheckForDoctor() && x.CheckForAlive())?.Id;
    }

    public List<long>? GetCivilians()
    {
        var civilians = _players.Where(x => x.CheckForCivilian() && x.CheckForAlive()).Select(x => x.Id).ToList();
        if(civilians.Count == 0) return null;
        return civilians;
    }

    public void MafiaWalks(long targetId)
    {
        var targetObj = _players.First(x => x.Id == targetId);
        if(targetObj == null) throw new ArgumentException("Data about mafia walk is null", nameof(targetObj));
        _mafiaTarget = targetObj;
    }
 

    public bool DetectiveWalks(long targetId)
    {
        var targetObj = _players.First(x => x.Id == targetId);
        if(targetObj == null) throw new ArgumentException("Data about detective walk is null", nameof(targetObj));
        return targetObj.CheckForMafia();
    }
    
    public void DoctorWalks(long targetId)
    {
        var targetObj = _players.First(x => x.Id == targetId);
        if(targetObj == null) throw new ArgumentException("Data about doctor walk is null", nameof(targetObj));
        _doctorTarget = targetObj;
    }

    public void KickPlayer(long targetId)
    {
        var targetObj = _players.First(x => x.Id == targetId);
        if(targetObj == null) throw new ArgumentException("Data about kicked player is null", nameof(targetObj));
        targetObj.Kill();
    }

    public long? CheckRoundResults()
    {
        if (_mafiaTarget == null) throw new ArgumentException();
        if(_players.FirstOrDefault(x => x.CheckForDoctor() && x.CheckForAlive()) != null)
        {
            if (_mafiaTarget.Equals(_doctorTarget))
            {
                return null;
            }
        }
        _players.First(x => x.Equals(_mafiaTarget)).Kill();
        return _mafiaTarget.Id;
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