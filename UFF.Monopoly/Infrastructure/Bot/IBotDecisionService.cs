using UFF.Monopoly.Entities;
using System.Collections.Generic;

namespace UFF.Monopoly.Infrastructure.Bot;

/// <summary>
/// Serviço de decisão do bot para o gameplay.
/// Centraliza regras de negócio e heurísticas para permitir evolução do algoritmo.
/// </summary>
public interface IBotDecisionService
{
    /// <summary>
    /// Determina se o jogador é um bot.
    /// </summary>
    /// <param name="player">Jogador a ser verificado.</param>
    /// <returns>Verdadeiro se o jogador é bot.</returns>
    bool IsBotPlayer(Player? player);

    /// <summary>
    /// Indica se o bot deve rolar os dados automaticamente nesta situação.
    /// </summary>
    /// <param name="game">Instância atual do jogo.</param>
    /// <param name="isTypingChat">Se há digitação ativa no chat.</param>
    /// <param name="isAnimating">Se há animação em andamento.</param>
    /// <param name="hasRolledThisTurn">Se já rolou nesse turno.</param>
    /// <returns>Verdadeiro se deve rolar automaticamente.</returns>
    bool ShouldAutoRoll(Game? game, bool isTypingChat, bool isAnimating, bool hasRolledThisTurn);

    /// <summary>
    /// Indica se o bot deve tentar comprar o bloco atual.
    /// </summary>
    /// <param name="block">Bloco atual do modal.</param>
    /// <param name="player">Jogador atual do modal.</param>
    /// <param name="game">Instância do jogo.</param>
    /// <returns>Verdadeiro se a compra é recomendada.</returns>
    bool ShouldBuy(Block? block, Player? player, Game? game);

    /// <summary>
    /// Indica se o bot deve tentar evoluir (upgrade) a propriedade atual.
    /// </summary>
    /// <param name="property">Propriedade alvo.</param>
    /// <param name="player">Jogador dono da propriedade.</param>
    /// <param name="game">Instância do jogo.</param>
    /// <returns>Verdadeiro se o upgrade é recomendado.</returns>
    bool ShouldUpgrade(PropertyBlock? property, Player? player, Game? game);

    /// <summary>
    /// Delay em milissegundos para animação de "pensando".
    /// </summary>
    int ThinkingDelayMs { get; }

    /// <summary>
    /// Delay em milissegundos antes de executar compra.
    /// </summary>
    int PurchaseDelayMs { get; }

    /// <summary>
    /// Delay em milissegundos antes de executar upgrade.
    /// </summary>
    int UpgradeDelayMs { get; }

    /// <summary>
    /// Delay em milissegundos antes de rolar dados automaticamente.
    /// </summary>
    int AutoRollDelayMs { get; }

    // NOVO (fase 1 simplificada)
    DecisionResult EvaluateTurnStart(DecisionContext ctx);
    List<DecisionResult> EvaluateModal(DecisionContext ctx);
}
