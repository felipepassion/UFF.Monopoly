namespace UFF.Monopoly.Components.Shared;

public static class PriceThresholds
{
    // Ajuste conforme balanceamento
    public const int CheapMax = 1500; // até este valor = verde
    public const int MediumMax = 6000; // até este valor = amarelo
    // acima de MediumMax = caro (vermelho)

    public static string GetPriceClass(int price)
        => price <= CheapMax ? "price-green" : price <= MediumMax ? "price-yellow" : "price-red";
}
