using System.Collections.ObjectModel;

namespace UFF.Monopoly.Entities;

public class Game
{
    public int BoardSize => _board.Count;
    public const int GoSalary = 200;
    public int CurrentPlayerIndex { get; private set; } = 0;
    public bool IsFinished { get; private set; } = false;

    public IReadOnlyList<Block> Board => _board;
    public IReadOnlyList<Player> Players => _players;

    private readonly List<Block> _board;
    private readonly List<Player> _players;

    public Game(IEnumerable<Player> players, IEnumerable<Block> board)
    {
        _players = players.ToList();
        _board = board.OrderBy(b => b.Position).ToList();
    }

    public (int die1, int die2, int total) RollDice(IRandomDice? dice = null)
    {
        dice ??= new RandomDice();
        var (d1, d2) = dice.Roll();
        return (d1, d2, d1 + d2);
    }

    public void NextTurn()
    {
        if (IsFinished) return;
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
        } while (_players[CurrentPlayerIndex].IsBankrupt);
    }

    public async Task MoveCurrentPlayerAsync(int steps)
    {
        var player = _players[CurrentPlayerIndex];
        if (player.IsBankrupt) { NextTurn(); return; }

        var oldPos = player.CurrentPosition;
        var looped = oldPos + steps >= BoardSize;
        var newPos = (oldPos + steps) % BoardSize;
        if (looped)
        {
            player.Money += GoSalary;
        }
        player.CurrentPosition = newPos;

        var block = _board.First(b => b.Position == newPos);
        await block.Action(this, player);

        if (player.Money < 0)
        {
            player.IsBankrupt = true;
            foreach (var prop in player.OwnedProperties)
            {
                prop.Owner = null;
                prop.IsMortgaged = false;
            }
            player.OwnedProperties.Clear();
        }
    }

    public bool TryBuyProperty(Player player, Block block)
    {
        if (block.Type != BlockType.Property || block.Owner != null || block.IsMortgaged)
            return false;
        if (player.Money < block.Price) return false;
        player.Money -= block.Price;
        block.Owner = player;
        player.OwnedProperties.Add(block);
        return true;
    }

    public void Transfer(Player from, Player to, int amount)
    {
        if (amount <= 0) return;
        from.Money -= amount;
        to.Money += amount;
    }

    public void PayBank(Player player, int amount)
    {
        if (amount <= 0) return;
        player.Money -= amount;
    }

    public void SendToJail(Player player)
    {
        player.InJail = true;
        player.JailTurns = 0;
        // On reduced board, jail is first found block of type Jail
        var jailPos = _board.First(b => b.Type == BlockType.Jail).Position;
        player.CurrentPosition = jailPos;
    }

    public async Task HandleJailAsync(Player player, IRandomDice? dice = null)
    {
        if (!player.InJail) return;
        dice ??= new RandomDice();
        if (player.GetOutOfJailFreeCards > 0)
        {
            player.GetOutOfJailFreeCards--;
            player.InJail = false;
            return;
        }

        var (d1, d2) = dice.Roll();
        if (d1 == d2)
        {
            player.InJail = false;
            await MoveCurrentPlayerAsync(d1 + d2);
            return;
        }

        player.JailTurns++;
        if (player.JailTurns >= 3)
        {
            const int fine = 50;
            PayBank(player, fine);
            player.InJail = false;
        }
    }
}

public interface IRandomDice
{
    (int d1, int d2) Roll();
}

public class RandomDice : IRandomDice
{
    private readonly Random _rng = new();
    public (int d1, int d2) Roll()
    {
        return (_rng.Next(1, 7), _rng.Next(1, 7));
    }
}
