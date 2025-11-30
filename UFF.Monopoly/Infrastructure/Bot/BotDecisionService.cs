using UFF.Monopoly.Entities;
using System.Collections.Generic;

namespace UFF.Monopoly.Infrastructure.Bot;

/// <summary>
/// Implementação padrão de heurísticas do bot.
/// Fase 1: mantém lógica antiga e adiciona novas funções estruturadas simples.
/// </summary>
public class BotDecisionService : IBotDecisionService
{
    /// <inheritdoc />
    public int ThinkingDelayMs => 1200;
    /// <inheritdoc />
    public int PurchaseDelayMs => 1200;
    /// <inheritdoc />
    public int UpgradeDelayMs => 1100;
    /// <inheritdoc />
    public int AutoRollDelayMs => 1200;

    /// <inheritdoc />
    public bool IsBotPlayer(Player? player)
        => player?.Name?.StartsWith("Bot ", StringComparison.OrdinalIgnoreCase) == true;

    /// <inheritdoc />
    public bool ShouldAutoRoll(Game? game, bool isTypingChat, bool isAnimating, bool hasRolledThisTurn)
    {
        if (game is null) return false;
        if (isTypingChat) return false;
        if (isAnimating) return false;
        if (hasRolledThisTurn) return false;
        var idx = game.CurrentPlayerIndex;
        if (idx < 0 || idx >= game.Players.Count) return false;
        return IsBotPlayer(game.Players[idx]);
    }

    /// <inheritdoc />
    public bool ShouldBuy(Block? block, Player? player, Game? game)
    {
        if (block is null || player is null || game is null) return false;
        if (block.Owner is not null) return false;
        if (block.Type != BlockType.Property && block.Type != BlockType.Company) return false;
        if (player.Money < block.Price) return false;
        // Heurística simples: comprar se tem dinheiro e é propriedade/empresa
        return true;
    }

    /// <inheritdoc />
    public bool ShouldUpgrade(PropertyBlock? property, Player? player, Game? game)
    {
        if (property is null || player is null || game is null) return false;
        if (property.Owner != player) return false;
        // Respeita regra de upgrade externa (Play.CanUpgradeAllowed)
        // Heurística básica: permitir upgrade quando possível
        return true;
    }

    // NOVO - fase 1 simplificada
    /// <summary>
    /// Avalia o início do turno para o jogador atual.
    /// </summary>
    public DecisionResult EvaluateTurnStart(DecisionContext ctx)
    {
        if (ctx.CurrentPlayer is null || ctx.Game is null) return DecisionResult.Simple(DecisionType.None, "Contexto incompleto");
        if (!IsBotPlayer(ctx.CurrentPlayer)) return DecisionResult.Simple(DecisionType.None, "Não é bot");
        if (ctx.HasRolledThisTurn) return DecisionResult.Simple(DecisionType.EndTurn, "Já rolou o dado", 5, 400);
        if (ctx.IsTypingChat || ctx.IsAnimating) return DecisionResult.Simple(DecisionType.Skip, "Aguardando UI livre", 1, 350);
        return DecisionResult.Simple(DecisionType.Roll, "Pronto para rolar", 10, AutoRollDelayMs);
    }

    /// <summary>
    /// Avalia as ações possíveis em uma modal (compra, upgrade) para o bloco atual.
    /// </summary>
    public List<DecisionResult> EvaluateModal(DecisionContext ctx)
    {
        var list = new List<DecisionResult>();
        if (ctx.CurrentPlayer is null || ctx.Game is null || ctx.CurrentBlock is null)
        { list.Add(DecisionResult.Simple(DecisionType.None, "Modal sem contexto")); return list; }
        if (!IsBotPlayer(ctx.CurrentPlayer)) { list.Add(DecisionResult.Simple(DecisionType.None, "Não é bot")); return list; }

        // Compra
        if (ShouldBuy(ctx.CurrentBlock, ctx.CurrentPlayer, ctx.Game))
            list.Add(DecisionResult.Simple(DecisionType.Buy, "Heurística básica de compra", 8, PurchaseDelayMs, target: ctx.CurrentBlock));

        // Upgrade
        if (ctx.CurrentBlock is PropertyBlock pb && ShouldUpgrade(pb, ctx.CurrentPlayer, ctx.Game))
            list.Add(DecisionResult.Simple(DecisionType.Upgrade, "Heurística básica de upgrade", 6, UpgradeDelayMs, target: ctx.CurrentBlock));

        // Caso nenhuma ação viável
        if (list.Count == 0)
            list.Add(DecisionResult.Simple(DecisionType.EndTurn, "Sem ação viável", 3, 500));
        else
            list.Add(DecisionResult.Simple(DecisionType.EndTurn, "Encerrar após ações", 1, 500));

        // Ordena por prioridade (desc)
        list.Sort((a,b) => b.Priority.CompareTo(a.Priority));
        return list;
    }
}
