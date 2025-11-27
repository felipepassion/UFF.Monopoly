using System.Collections.ObjectModel;

namespace UFF.Monopoly.Entities;

public class Game
{
    public int BoardSize => _board.Count;
    public const int GoSalary = 200;
    // Allow restoring persisted index
    public int CurrentPlayerIndex { get; set; } = 0;
    public bool IsFinished { get; private set; } = false;

    // number of completed rounds (incremented when we wrap back to player 0)
    public int RoundCount { get; private set; } = 0;

    // Flag set true when the current player's movement passed the Go block
    public bool PassedGoThisMove { get; private set; } = false;

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
        // Reset pass-go flag at the start of the next player's turn
        PassedGoThisMove = false;

        int attempts = 0;
        do
        {
            var previous = CurrentPlayerIndex;
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;

            // if we wrapped to player 0, increment round count
            if (CurrentPlayerIndex == 0 && previous != 0)
            {
                RoundCount++;
            }

            // skip players that are bankrupt
            if (_players[CurrentPlayerIndex].IsBankrupt) { attempts++; continue; }

            // skip players that have pending SkipTurns
            if (_players[CurrentPlayerIndex].SkipTurns > 0)
            {
                _players[CurrentPlayerIndex].SkipTurns--;
                attempts++;
                continue;
            }

            break;
        } while (attempts < _players.Count);
    }

    public async Task MoveCurrentPlayerAsync(int steps)
    {
        var player = _players[CurrentPlayerIndex];
        if (player.IsBankrupt) { NextTurn(); return; }

        var oldPos = player.CurrentPosition;
        var newPos = (oldPos + steps) % BoardSize;

        // Determine whether the player actually crosses a Go block during this move.
        // Iterate each traversed position (exclusive of oldPos, inclusive of newPos) and
        // award GoSalary only if we encounter a block whose Type == BlockType.Go.
        PassedGoThisMove = false;
        for (int i = 1; i <= steps; i++)
        {
            var pos = (oldPos + i) % BoardSize;
            var traversed = _board.FirstOrDefault(b => b.Position == pos);
            if (traversed != null && traversed.Type == BlockType.Go)
            {
                player.Money += GoSalary;
                PassedGoThisMove = true;
                break; // award only once per move
            }
        }

        player.CurrentPosition = newPos;

        // Only execute the action for the final block where the player stopped.
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
        // allow buying Property or Company
        if ((block.Type != BlockType.Property && block.Type != BlockType.Company) || block.Owner != null || block.IsMortgaged)
            return false;
        if (player.Money < block.Price) return false;
        player.Money -= block.Price;
        block.Owner = player;
        player.OwnedProperties.Add(block);

        player.LastPurchaseTurn = RoundCount;

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
        // default to 1 turn if specific duration is not provided elsewhere
        SendToJail(player, 1);
    }

    public void SendToJail(Player player, int turns)
    {
        // Simplified jail: no visiting space movement. Only mark status and skip turns.
        player.InJail = true;
        player.JailTurns = 0;
        player.SkipTurns = Math.Max(0, turns);
        // Do not move the player to a dedicated jail block anymore.
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

    // Mark game as finished
    public void Finish()
    {
        IsFinished = true;
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
