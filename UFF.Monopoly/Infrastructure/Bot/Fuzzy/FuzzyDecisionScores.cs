namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Armazena as pontuações fuzzy (forças de regra) para as ações possíveis: Comprar, Vender, Esperar.
/// Cada pontuação corresponde ao grau de pertinência do saldo nas categorias HIGH, LOW e AVERAGE.
/// </summary>
public sealed class FuzzyDecisionScores
{
    /// <summary>
    /// Pontuação para a ação Comprar, derivada de μ_high(x).
    /// </summary>
    public double BuyScore { get; set; }

    /// <summary>
    /// Pontuação para a ação Vender, derivada de μ_low(x).
    /// </summary>
    public double SellScore { get; set; }

    /// <summary>
    /// Pontuação para a ação Esperar, derivada de μ_avg(x).
    /// </summary>
    public double WaitScore { get; set; }
}
