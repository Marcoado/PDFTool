using PdfTool.Core;

namespace PdfTool.Tests;

public sealed class UnificadorDePdfTests
{
    private readonly UnificadorDePdf _unificador = new();

    [Fact]
    public void Unificar_RespeitaAOrdemDasPaginas()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 210, 220);
        FabricaDePdf.Criar(pasta.Arquivo("b.pdf"), 310);

        var resultado = _unificador.Unificar(pasta.Caminho,
        [
            new PaginaPedida("b.pdf", 1),
            new PaginaPedida("a.pdf", 2),
            new PaginaPedida("a.pdf", 1)
        ]);

        Assert.Equal("unificado.pdf", resultado.Arquivo);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal([310d, 220d, 210d], FabricaDePdf.Larguras(resultado.Caminho));
    }

    [Fact]
    public void Unificar_NaoSobrescreveArquivoExistente()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 200);
        File.WriteAllBytes(pasta.Arquivo("unificado.pdf"), [1, 2, 3]);

        var resultado = _unificador.Unificar(pasta.Caminho, [new PaginaPedida("a.pdf", 1)]);

        Assert.Equal("unificado-2.pdf", resultado.Arquivo);
        Assert.Equal([1, 2, 3], File.ReadAllBytes(pasta.Arquivo("unificado.pdf")));
        Assert.Equal([200d], FabricaDePdf.Larguras(resultado.Caminho));
    }

    [Fact]
    public void UnificarCaminhos_IncluiTodasAsPaginasNaOrdemDosArquivos()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 210, 220);
        FabricaDePdf.Criar(pasta.Arquivo("b.pdf"), 310);

        var resultado = _unificador.UnificarCaminhos(pasta.Caminho,
        [
            pasta.Arquivo("b.pdf"),
            pasta.Arquivo("a.pdf")
        ]);

        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal([310d, 210d, 220d], FabricaDePdf.Larguras(resultado.Caminho));
    }

    [Theory]
    [InlineData(@"..\fora.pdf")]
    [InlineData("pasta/arquivo.pdf")]
    [InlineData("ausente.pdf")]
    public void Unificar_ArquivoForaDaPasta_Recusa(string arquivo)
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 200);

        var erro = Assert.Throws<PedidoInvalidoException>(() =>
            _unificador.Unificar(pasta.Caminho, [new PaginaPedida(arquivo, 1)]));

        Assert.False(string.IsNullOrWhiteSpace(erro.Message));
    }

    [Fact]
    public void Unificar_PaginaInexistente_Recusa()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 200);

        Assert.Throws<PedidoInvalidoException>(() =>
            _unificador.Unificar(pasta.Caminho, [new PaginaPedida("a.pdf", 4)]));
    }

    [Fact]
    public void Unificar_SemPaginas_Recusa()
    {
        using var pasta = new PastaTemporaria();

        Assert.Throws<PedidoInvalidoException>(() =>
            _unificador.Unificar(pasta.Caminho, []));
    }
}
