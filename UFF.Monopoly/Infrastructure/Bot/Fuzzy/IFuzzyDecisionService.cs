namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Serviço de decisão fuzzy (Mamdani simplificado) para o bot.
/// Fornece ações Buy/Sell/Wait com pontuações e explicações, baseado no saldo atual.
/// </summary>
public interface IFuzzyDecisionService
{
    /// <summary>
    /// Avalia o saldo atual e retorna a decisão crisp com pontuações e explicação.
    /// </summary>
    FuzzyDecisionResult Evaluate(double balance);

    /// <summary>
    /// Aplica um imposto T (taxa) ao saldo e reavalia: B_new = max(0, B_old - T).
    /// </summary>
    FuzzyDecisionResult ApplyTaxAndDecide(double balance, double taxAmount);

    /// <summary>
    /// Aplica um bônus R ao saldo e reavalia: B_new = min(3000, B_old + R).
    /// </summary>
    FuzzyDecisionResult ApplyBonusAndDecide(double balance, double bonusAmount);
}
