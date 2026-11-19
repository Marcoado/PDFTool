using PdfSharp.Pdf.IO;

namespace PdfTool.Core;

/// <summary>
/// Lê os PDFs de uma pasta e lista cada página.
/// </summary>
public sealed class LeitorDePasta
{
    /// <summary>
    /// Lê os arquivos <c>.pdf</c> que estão diretamente na pasta.
    /// </summary>
    /// <param name="pasta">A pasta escolhida pelo usuário.</param>
    /// <returns>As páginas em ordem de nome de arquivo e, dentro de cada arquivo, a ordem original.</returns>
    /// <exception cref="PedidoInvalidoException">A pasta não foi informada ou não existe.</exception>
    public ResultadoDeLeitura Ler(string pasta)
    {
        if (string.IsNullOrWhiteSpace(pasta))
            throw new PedidoInvalidoException("Escolha uma pasta.");

        var raiz = Path.GetFullPath(pasta);
        if (!Directory.Exists(raiz))
            throw new PedidoInvalidoException("A pasta não existe.");

        var avisos = new List<string>();
        var paginas = new List<PaginaDeOrigem>();
        var arquivos = ArquivosPdf.Listar(raiz);

        foreach (var arquivo in arquivos)
        {
            var nome = Path.GetFileName(arquivo);
            try
            {
                using var documento = PdfReader.Open(arquivo, PdfDocumentOpenMode.Import);
                if (documento.PageCount == 0)
                {
                    avisos.Add($"{nome} não tem páginas e foi ignorado.");
                    continue;
                }

                for (var numero = 1; numero <= documento.PageCount; numero++)
                    paginas.Add(new PaginaDeOrigem(nome, numero, documento.PageCount));
            }
            catch (Exception excecao) when (excecao is PdfReaderException
                                             or IOException
                                             or UnauthorizedAccessException
                                             or InvalidOperationException
                                             or ArgumentException)
            {
                avisos.Add($"{nome} foi ignorado: {excecao.Message}");
            }
        }

        return new ResultadoDeLeitura(raiz, paginas, avisos);
    }
}
