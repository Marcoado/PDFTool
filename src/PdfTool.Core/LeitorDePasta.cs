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

    /// <summary>
    /// Lê a pasta e devolve um item por PDF.
    /// </summary>
    /// <param name="pasta">A pasta escolhida pelo usuário.</param>
    /// <returns>Os arquivos da pasta, em ordem de nome.</returns>
    /// <exception cref="PedidoInvalidoException">A pasta não foi informada ou não existe.</exception>
    public ResultadoDeArquivos LerPasta(string pasta)
    {
        if (string.IsNullOrWhiteSpace(pasta))
            throw new PedidoInvalidoException("Escolha uma pasta.");

        var raiz = Path.GetFullPath(pasta);
        if (!Directory.Exists(raiz))
            throw new PedidoInvalidoException("A pasta não existe.");

        var caminhos = ArquivosPdf.Listar(raiz);
        if (caminhos.Count == 0)
            return new ResultadoDeArquivos(raiz, [], []);

        return LerArquivos(caminhos, raiz);
    }

    /// <summary>
    /// Lê os PDFs informados e devolve um item por arquivo.
    /// </summary>
    /// <param name="caminhos">Os arquivos escolhidos, na ordem desejada.</param>
    /// <param name="pastaSaida">A pasta onde o PDF unificado será gravado.</param>
    /// <returns>Os arquivos válidos, sem repetir o mesmo PDF.</returns>
    /// <exception cref="PedidoInvalidoException">Nenhum arquivo foi informado ou a pasta de saída não existe.</exception>
    public ResultadoDeArquivos LerArquivos(IReadOnlyList<string> caminhos, string pastaSaida)
    {
        if (caminhos is null || caminhos.Count == 0)
            throw new PedidoInvalidoException("Escolha ao menos um PDF.");

        var saida = Path.GetFullPath(pastaSaida);
        if (!Directory.Exists(saida))
            throw new PedidoInvalidoException("A pasta de saída não existe.");

        var avisos = new List<string>();
        foreach (var caminho in caminhos)
        {
            if (string.IsNullOrWhiteSpace(caminho))
                continue;

            var nome = Path.GetFileName(caminho.Trim());
            if (!nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                avisos.Add($"{nome} foi ignorado porque não é um PDF.");
        }

        var arquivos = new List<ArquivoLido>();
        foreach (var caminho in ArquivosPdf.SemRepetidos(caminhos, ArquivosPdf.Identidade))
        {
            var nome = Path.GetFileName(caminho);
            try
            {
                using var documento = PdfReader.Open(caminho, PdfDocumentOpenMode.Import);
                if (documento.PageCount == 0)
                {
                    avisos.Add($"{nome} não tem páginas e foi ignorado.");
                    continue;
                }

                var pasta = Path.GetDirectoryName(caminho) ?? saida;
                arquivos.Add(new ArquivoLido(nome, documento.PageCount, caminho, pasta));
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

        return new ResultadoDeArquivos(saida, arquivos, avisos);
    }
}
