using System.ComponentModel.DataAnnotations;

namespace UFF.Monopoly.Models;

public class BoardFormModel
{
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Range(2, 30, ErrorMessage = "Linhas deve estar entre 2 e 30")]
    public int Rows { get; set; } = 5;

    [Range(2, 30, ErrorMessage = "Colunas deve estar entre 2 e 30")]
    public int Cols { get; set; } = 5;

    [Range(20, 200, ErrorMessage = "Tamanho da célula deve estar entre 20 e 200")]
    public int CellSize { get; set; } = 64;

    // Center image url stored with the board
    public string? CenterImageUrl { get; set; } = "/images/mr_monopoly/mr_monopoly_with_chat_and_scenario.png";
}
