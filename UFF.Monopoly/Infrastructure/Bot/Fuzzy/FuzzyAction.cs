namespace UFF.Monopoly.Infrastructure.Bot.Fuzzy;

/// <summary>
/// Representa a ação crisp resultante do processo de decisão fuzzy para o bot.
/// BUY (comprar), SELL (vender) ou WAIT (esperar), conforme as regras e pontuações.
/// </summary>
public enum FuzzyAction
{
    /// <summary>
    /// Comprar um ativo/propriedade, tipicamente recomendado com saldo alto.
    /// </summary>
    Buy,
    /// <summary>
    /// Vender um ativo/propriedade, tipicamente recomendado com saldo baixo.
    /// </summary>
    Sell,
    /// <summary>
    /// Aguardar a próxima oportunidade, tipicamente recomendado com saldo médio.
    /// </summary>
    Wait
}
