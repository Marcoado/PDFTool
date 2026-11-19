using PdfTool.Core;

namespace PdfTool.Tests;

public sealed class NomesDeSaidaTests
{
    [Fact]
    public void ProximoUnificado_PastaVazia_UsaNomeBase()
    {
        using var pasta = new PastaTemporaria();

        var caminho = NomesDeSaida.ProximoUnificado(pasta.Caminho);

        Assert.Equal(Path.Combine(pasta.Caminho, "unificado.pdf"), caminho);
    }

    [Fact]
    public void ProximoUnificado_NomeOcupado_Incrementa()
    {
        using var pasta = new PastaTemporaria();
        File.WriteAllBytes(pasta.Arquivo("unificado.pdf"), [1]);
        File.WriteAllBytes(pasta.Arquivo("unificado-2.pdf"), [1]);

        var caminho = NomesDeSaida.ProximoUnificado(pasta.Caminho);

        Assert.Equal(Path.Combine(pasta.Caminho, "unificado-3.pdf"), caminho);
    }
}
