namespace UFF.Monopoly.Constants;

public static class PlayerColors
{
    // Paleta fixa de cores (hex) para até 8 jogadores
    // Ordem pensada para contraste: azul, verde, laranja, vermelho, roxo, rosa, ciano, amarelo
    public static readonly string[] Colors =
    {
        "#1d4ed8", // P0 - Azul
        "#10b981", // P1 - Verde
        "#f59e0b", // P2 - Laranja
        "#dc2626", // P3 - Vermelho
        "#8b5cf6", // P4 - Roxo
        "#ec4899", // P5 - Rosa
        "#06b6d4", // P6 - Ciano
        "#eab308"  // P7 - Amarelo
    };

    // Retorna cor (hex) para índice de jogador (com fallback)
    public static string Get(int index) => index >= 0 && index < Colors.Length ? Colors[index] : "#6b7280"; // cinza fallback
}
