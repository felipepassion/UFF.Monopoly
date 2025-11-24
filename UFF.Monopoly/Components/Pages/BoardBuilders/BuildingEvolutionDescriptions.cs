using System.Collections.ObjectModel;
using UFF.Monopoly.Entities;

namespace UFF.Monopoly.Components.Pages.BoardBuilders;

public static class BuildingEvolutionDescriptions
{
    public sealed record EvolutionInfo(string Name,string Description);

    // Estrutura: BuildingType -> nível -> info
    private static readonly ReadOnlyDictionary<BuildingType, EvolutionInfo[]> _map =
        new(new Dictionary<BuildingType, EvolutionInfo[]>
        {
            [BuildingType.Hotel] = new[]
            {
                new EvolutionInfo("Terreno","Um lote vazio preparado para futuras construções destinadas a hospedagem."),
                new EvolutionInfo("Airbnb","Pequenas unidades de aluguel de curto prazo gerando fluxo inicial de hóspedes."),
                new EvolutionInfo("Pousada","Estabelecimento acolhedor com quartos confortáveis e serviços básicos de hotelaria."),
                new EvolutionInfo("Mansão","Propriedade luxuosa transformada em estadia premium de altíssimo padrão."),
            },
            [BuildingType.Company] = new[]
            {
                new EvolutionInfo("Terreno","Área reservada para futura expansão corporativa."),
                new EvolutionInfo("Escritório","Primeira sede com poucas salas administrativas."),
                new EvolutionInfo("Centro Comercial","Conjunto de operações coordenadas com departamentos e infraestrutura ampliada."),
                new EvolutionInfo("Empresa","Corporação estabelecida com marca forte e receita elevada."),
            },
            [BuildingType.House] = new[]
            {
                new EvolutionInfo("Terreno","Lote residencial vazio aguardando construção."),
                new EvolutionInfo("Flat","Unidade compacta moderna com serviços básicos e baixo custo de manutenção."),
                new EvolutionInfo("Casa","Residência confortável de médio porte atraindo moradores mais estáveis."),
                new EvolutionInfo("Mansão","Residência luxuosa de alto padrão elevando significativamente o valor da área."),
            },
            [BuildingType.Special] = new[]
            {
                new EvolutionInfo("Circo","Espetáculos itinerantes que começam a atrair visitantes ocasionais."),
                new EvolutionInfo("Shopping","Grande complexo comercial diversificado com lojas e entretenimento."),
                new EvolutionInfo("Estádio","Arena multiuso para eventos esportivos e grandes shows, gerando alto fluxo."),
                new EvolutionInfo("Aeroporto","Infraestrutura estratégica conectando a região nacional e internacionalmente."),
            },
        });

    public static EvolutionInfo Get(BuildingType type,int level)
    {
        if (!_map.TryGetValue(type,out var list) || level < 1 || level > list.Length)
            return new EvolutionInfo("Nível desconhecido","Não há descrição disponível.");
        return list[level-1];
    }
}
