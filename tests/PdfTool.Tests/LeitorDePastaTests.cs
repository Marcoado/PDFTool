using PdfTool.Core;

namespace PdfTool.Tests;

public sealed class LeitorDePastaTests
{
    private readonly LeitorDePasta _leitor = new();

    [Fact]
    public void Ler_OrdenaArquivosENumeraPaginas()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("b.pdf"), 300, 310);
        FabricaDePdf.Criar(pasta.Arquivo("a.pdf"), 200);
        var subpasta = Path.Combine(pasta.Caminho, "anexos");
        Directory.CreateDirectory(subpasta);
        FabricaDePdf.Criar(Path.Combine(subpasta, "escondido.pdf"), 900);
        File.WriteAllText(pasta.Arquivo("notas.txt"), "não é pdf");

        var resultado = _leitor.Ler(pasta.Caminho);

        Assert.Equal(
            new[] { "a.pdf:1", "b.pdf:1", "b.pdf:2" },
            resultado.Paginas.Select(pagina => $"{pagina.Arquivo}:{pagina.Pagina}").ToArray());
        Assert.Equal(2, resultado.Paginas[1].TotalNoArquivo);
        Assert.Empty(resultado.Avisos);
    }

    [Fact]
    public void Ler_ArquivoInvalido_ViraAvisoEMantemOsDemais()
    {
        using var pasta = new PastaTemporaria();
        File.WriteAllText(pasta.Arquivo("quebrado.pdf"), "isto não é um pdf");
        FabricaDePdf.Criar(pasta.Arquivo("bom.pdf"), 220);

        var resultado = _leitor.Ler(pasta.Caminho);

        Assert.Equal("bom.pdf", Assert.Single(resultado.Paginas).Arquivo);
        Assert.Contains(resultado.Avisos, aviso => aviso.StartsWith("quebrado.pdf foi ignorado:", StringComparison.Ordinal));
    }

    [Fact]
    public void Ler_NomeLongo_EntraUmaVez()
    {
        using var pasta = new PastaTemporaria();
        FabricaDePdf.Criar(pasta.Arquivo("contrato-assinado.pdf"), 200, 210);

        var resultado = _leitor.Ler(pasta.Caminho);

        Assert.Equal(["contrato-assinado.pdf:1", "contrato-assinado.pdf:2"],
            resultado.Paginas.Select(pagina => $"{pagina.Arquivo}:{pagina.Pagina}").ToArray());
    }

    [Fact]
    public void SemRepetidos_CaminhoCurtoELongoDoMesmoArquivo_MantemUm()
    {
        var unicos = ArquivosPdf.SemRepetidos(
            [@"C:\docs\RELATO~1.PDF", @"C:\docs\relatorio-final.pdf", @"C:\docs\notas.txt"],
            caminho => caminho.Contains('~')
                ? @"C:\docs\relatorio-final.pdf"
                : caminho);

        Assert.Equal([@"C:\docs\relatorio-final.pdf"], unicos);
    }
}
