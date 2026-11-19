using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfTool.Tests;

internal static class FabricaDePdf
{
    public static void Criar(string caminho, params double[] larguras)
    {
        using var documento = new PdfDocument();
        foreach (var largura in larguras)
        {
            var pagina = documento.AddPage();
            pagina.Width = XUnit.FromPoint(largura);
            pagina.Height = XUnit.FromPoint(840);
        }

        documento.Save(caminho);
    }

    public static double[] Larguras(string caminho)
    {
        using var documento = PdfReader.Open(caminho, PdfDocumentOpenMode.Import);
        var larguras = new double[documento.PageCount];
        for (var indice = 0; indice < documento.PageCount; indice++)
            larguras[indice] = documento.Pages[indice].Width.Point;

        return larguras;
    }
}

internal sealed class PastaTemporaria : IDisposable
{
    public PastaTemporaria()
    {
        Caminho = Path.Combine(Path.GetTempPath(), "pdftool-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Caminho);
    }

    public string Caminho { get; }

    public string Arquivo(string nome) => Path.Combine(Caminho, nome);

    public void Dispose()
    {
        if (Directory.Exists(Caminho))
            Directory.Delete(Caminho, recursive: true);
    }
}
