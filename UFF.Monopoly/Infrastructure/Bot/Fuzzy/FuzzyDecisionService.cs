using System;

namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Implementação de um mecanismo fuzzy (Mamdani simplificado) para decisões de compra/venda/espera.
/// - Universo: saldo B ∈ [0, 3000]; normalizado x ∈ [0, 100] por x = (B/3000)*100.
/// - Conjuntos fuzzy LOW, AVERAGE, HIGH com funções de pertinência triangulares/trapezoidais.
/// - Regras: HIGH⇒BUY, AVERAGE⇒WAIT, LOW⇒SELL; a força da regra é o grau de pertinência correspondente.
/// - Decisão crisp por argmax das pontuações.
/// </summary>
public sealed class FuzzyDecisionService : IFuzzyDecisionService
{
    /// <summary>
    /// Avalia o saldo atual e retorna a ação recomendada, pontuações e uma explicação em pt-BR.
    /// </summary>
    public FuzzyDecisionResult Evaluate(double balance)
    {
        var x = Normalize(balance);
        var muLow = MuLow(x);
        var muAvg = MuAverage(x);
        var muHigh = MuHigh(x);

        // Regras (Mamdani simplificado)
        var scores = new FuzzyDecisionScores
        {
            BuyScore = Clamp01(muHigh),
            SellScore = Clamp01(muLow),
            WaitScore = Clamp01(muAvg)
        };

        var action = ArgMax(scores);
        var explanation = BuildExplanationPtBr(balance, x, scores, action);

        return new FuzzyDecisionResult
        {
            Action = action,
            Scores = scores,
            NormalizedBalance = x,
            ExplanationPtBr = explanation
        };
    }

    /// <summary>
    /// Aplica um imposto T (T ≥ 0): B_new = max(0, B_old - T), e reavalia.
    /// </summary>
    public FuzzyDecisionResult ApplyTaxAndDecide(double balance, double taxAmount)
    {
        var adjusted = Math.Max(0.0, balance - Math.Max(0.0, taxAmount));
        return Evaluate(adjusted);
    }

    /// <summary>
    /// Aplica um bônus R (R ≥ 0): B_new = min(3000, B_old + R), e reavalia.
    /// </summary>
    public FuzzyDecisionResult ApplyBonusAndDecide(double balance, double bonusAmount)
    {
        var adjusted = Math.Min(3000.0, balance + Math.Max(0.0, bonusAmount));
        return Evaluate(adjusted);
    }

    // --- Matemática ---

    /// <summary>
    /// Normaliza o saldo para x ∈ [0, 100] via x = (balance / 3000.0) * 100.0.
    /// </summary>
    private static double Normalize(double balance)
    {
        var x = (balance / 3000.0) * 100.0;
        return Math.Clamp(x, 0.0, 100.0);
    }

    /// <summary>
    /// μ_low(x): 1 se x ≤ 25; (50-x)/25 se 25 < x < 50; 0 se x ≥ 50.
    /// </summary>
    private static double MuLow(double x)
    {
        if (x <= 25.0) return 1.0;
        if (x >= 50.0) return 0.0;
        return (50.0 - x) / 25.0; // x ∈ (25,50)
    }

    /// <summary>
    /// μ_avg(x): 0 se x ≤ 25 ou x ≥ 75; (x-25)/25 se 25 < x ≤ 50; (75-x)/25 se 50 < x < 75.
    /// </summary>
    private static double MuAverage(double x)
    {
        if (x <= 25.0 || x >= 75.0) return 0.0;
        if (x <= 50.0) return (x - 25.0) / 25.0; // x ∈ (25,50]
        return (75.0 - x) / 25.0; // x ∈ (50,75)
    }

    /// <summary>
    /// μ_high(x): 0 se x ≤ 50; (x-50)/25 se 50 < x < 75; 1 se x ≥ 75.
    /// </summary>
    private static double MuHigh(double x)
    {
        if (x <= 50.0) return 0.0;
        if (x >= 75.0) return 1.0;
        return (x - 50.0) / 25.0; // x ∈ (50,75)
    }

    /// <summary>
    /// Seleciona a ação por argmax das pontuações.
    /// </summary>
    private static FuzzyAction ArgMax(FuzzyDecisionScores s)
    {
        if (s.BuyScore >= s.SellScore && s.BuyScore >= s.WaitScore) return FuzzyAction.Buy;
        if (s.SellScore >= s.BuyScore && s.SellScore >= s.WaitScore) return FuzzyAction.Sell;
        return FuzzyAction.Wait;
    }

    /// <summary>
    /// Garante que o valor esteja em [0,1].
    /// </summary>
    private static double Clamp01(double v) => Math.Clamp(v, 0.0, 1.0);

    /// <summary>
    /// Monta uma explicação em português, descrevendo o saldo, normalização e comparação de pontuações.
    /// </summary>
    private static string BuildExplanationPtBr(double balance, double x, FuzzyDecisionScores s, FuzzyAction a)
    {
        var actionStr = a switch
        {
            FuzzyAction.Buy => "COMPRAR",
            FuzzyAction.Sell => "VENDER",
            _ => "ESPERAR"
        };

        return $"Saldo B={balance:0.##} (x={x:0.##} em [0,100]). Pontuações: Buy={s.BuyScore:0.###}, Sell={s.SellScore:0.###}, Wait={s.WaitScore:0.###}. Decisão: {actionStr} pelo maior grau de pertinência.";
    }
}
