namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Resultado completo da decisão fuzzy: ação escolhida (crisp), pontuações parciais e explicação em pt-BR.
/// </summary>
public sealed class FuzzyDecisionResult
{
    /// <summary>
    /// Ação crisp selecionada pelo argmax das pontuações: Buy, Sell ou Wait.
    /// </summary>
    public FuzzyAction Action { get; set; }

    /// <summary>
    /// Estrutura com as pontuações de cada ação.
    /// </summary>
    public FuzzyDecisionScores Scores { get; set; } = new();

    /// <summary>
    /// Valor normalizado x em [0, 100] do saldo após ajustes.
    /// </summary>
    public double NormalizedBalance { get; set; }

    /// <summary>
    /// Explicação em português justificando a decisão, comparando pontuações.
    /// </summary>
    public string ExplanationPtBr { get; set; } = string.Empty;
}
