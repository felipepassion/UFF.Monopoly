namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Modelo de entrada para o mecanismo fuzzy, contendo o saldo atual e um helper para o valor normalizado.
/// </summary>
public sealed class FuzzyInput
{
    /// <summary>
    /// Saldo atual do jogador (B) no universo [0, 3000].
    /// </summary>
    public double Balance { get; set; }

    /// <summary>
    /// Valor normalizado x em [0, 100], calculado por x = (Balance / 3000.0) * 100.0.
    /// </summary>
    public double NormalizedBalance => (Balance / 3000.0) * 100.0;
}
