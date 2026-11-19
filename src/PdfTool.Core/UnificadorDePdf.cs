using System.Runtime.ExceptionServices;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfTool.Core;

/// <summary>
/// Grava um único PDF com as páginas na ordem pedida.
/// </summary>
public sealed class UnificadorDePdf
{
    /// <summary>
    /// Une as páginas e grava <c>unificado.pdf</c> na pasta, sem sobrescrever um arquivo existente.
    /// </summary>
    /// <param name="pasta">A pasta escolhida pelo usuário.</param>
    /// <param name="ordem">As páginas, na ordem em que devem aparecer no arquivo final.</param>
    /// <returns>O arquivo gerado.</returns>
    /// <exception cref="PedidoInvalidoException">A ordem está vazia, cita um arquivo de fora da pasta ou uma página inexistente.</exception>
    /// <exception cref="FalhaDeOperacaoException">O PDF não pôde ser gravado.</exception>
    public ResultadoDeUnificacao Unificar(string pasta, IReadOnlyList<PaginaPedida> ordem)
    {
        if (string.IsNullOrWhiteSpace(pasta))
            throw new PedidoInvalidoException("Escolha uma pasta.");

        if (ordem is null || ordem.Count == 0)
            throw new PedidoInvalidoException("Inclua ao menos uma página.");

        var raiz = Path.GetFullPath(pasta);
        if (!Directory.Exists(raiz))
            throw new PedidoInvalidoException("A pasta não existe.");

        var destino = NomesDeSaida.ProximoUnificado(raiz);
        var abertos = new List<PdfDocument>();
        var porArquivo = new Dictionary<string, PdfDocument>(StringComparer.OrdinalIgnoreCase);
        PdfDocument? saida = null;
        var gravou = false;
        ExceptionDispatchInfo? falha = null;

        try
        {
            saida = new PdfDocument();
            saida.Info.Title = "PDF unificado";
            saida.Info.Creator = "Folio";

            foreach (var item in ordem)
            {
                var nome = ValidarNome(item.Arquivo);
                if (item.Pagina < 1)
                    throw new PedidoInvalidoException($"A página de {nome} precisa ser 1 ou maior.");

                var caminho = Path.GetFullPath(Path.Combine(raiz, nome));
                if (!Caminhos.EhArquivoDaPasta(raiz, caminho) || !File.Exists(caminho))
                    throw new PedidoInvalidoException($"O arquivo {nome} não está na pasta escolhida.");

                if (!porArquivo.TryGetValue(caminho, out var origem))
                {
                    origem = PdfReader.Open(caminho, PdfDocumentOpenMode.Import);
                    porArquivo.Add(caminho, origem);
                    abertos.Add(origem);
                }

                if (item.Pagina > origem.PageCount)
                    throw new PedidoInvalidoException($"{nome} só tem {origem.PageCount} página(s).");

                saida.AddPage(origem.Pages[item.Pagina - 1]);
            }

            saida.Save(destino);
            gravou = true;
        }
        catch (Exception excecao)
        {
            falha = ExceptionDispatchInfo.Capture(Traduzir(excecao));
        }
        finally
        {
            saida?.Dispose();
            foreach (var documento in abertos)
                documento.Dispose();
        }

        if (!gravou && File.Exists(destino))
        {
            try
            {
                File.Delete(destino);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        falha?.Throw();
        return new ResultadoDeUnificacao(Path.GetFileName(destino), destino, ordem.Count);
    }

    /// <summary>
    /// Une os arquivos inteiros, na ordem recebida, gravando todas as páginas de cada um.
    /// </summary>
    /// <param name="pastaSaida">A pasta onde o PDF unificado será gravado.</param>
    /// <param name="caminhos">Os PDFs, na ordem do arquivo final.</param>
    /// <returns>O arquivo gerado.</returns>
    /// <exception cref="PedidoInvalidoException">A lista está vazia ou algum arquivo não pode ser lido.</exception>
    /// <exception cref="FalhaDeOperacaoException">O PDF não pôde ser gravado.</exception>
    public ResultadoDeUnificacao UnificarCaminhos(string pastaSaida, IReadOnlyList<string> caminhos)
    {
        if (string.IsNullOrWhiteSpace(pastaSaida))
            throw new PedidoInvalidoException("Escolha uma pasta.");

        if (caminhos is null || caminhos.Count == 0)
            throw new PedidoInvalidoException("Inclua ao menos um arquivo.");

        var raiz = Path.GetFullPath(pastaSaida);
        if (!Directory.Exists(raiz))
            throw new PedidoInvalidoException("A pasta não existe.");

        var destino = NomesDeSaida.ProximoUnificado(raiz);
        var abertos = new List<PdfDocument>();
        PdfDocument? saida = null;
        var gravou = false;
        var total = 0;
        ExceptionDispatchInfo? falha = null;

        try
        {
            saida = new PdfDocument();
            saida.Info.Title = "PDF unificado";
            saida.Info.Creator = "Folio";

            foreach (var caminho in caminhos)
            {
                var cheio = Path.GetFullPath(caminho);
                var nome = Path.GetFileName(cheio);
                if (!File.Exists(cheio) || !nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    throw new PedidoInvalidoException($"O arquivo {nome} não está disponível.");

                var origem = PdfReader.Open(cheio, PdfDocumentOpenMode.Import);
                abertos.Add(origem);
                if (origem.PageCount == 0)
                    throw new PedidoInvalidoException($"{nome} não tem páginas.");

                for (var indice = 0; indice < origem.PageCount; indice++)
                    saida.AddPage(origem.Pages[indice]);

                total += origem.PageCount;
            }

            saida.Save(destino);
            gravou = true;
        }
        catch (Exception excecao)
        {
            falha = ExceptionDispatchInfo.Capture(Traduzir(excecao));
        }
        finally
        {
            saida?.Dispose();
            foreach (var documento in abertos)
                documento.Dispose();
        }

        if (!gravou && File.Exists(destino))
        {
            try
            {
                File.Delete(destino);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        falha?.Throw();
        return new ResultadoDeUnificacao(Path.GetFileName(destino), destino, total);
    }

    private static Exception Traduzir(Exception excecao)
    {
        if (excecao is PedidoInvalidoException or FalhaDeOperacaoException)
            return excecao;

        if (excecao is PdfReaderException or IOException or UnauthorizedAccessException)
            return new FalhaDeOperacaoException($"Não foi possível unir os PDFs: {excecao.Message}", excecao);

        return excecao;
    }

    private static string ValidarNome(string? arquivo)
    {
        if (string.IsNullOrWhiteSpace(arquivo))
            throw new PedidoInvalidoException("Há uma página sem arquivo.");

        var nome = arquivo.Trim();
        if (!string.Equals(nome, Path.GetFileName(nome), StringComparison.Ordinal)
            || nome.Contains("..", StringComparison.Ordinal)
            || nome.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new PedidoInvalidoException("O nome do arquivo é inválido.");
        }

        if (!nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new PedidoInvalidoException("Só é possível unir arquivos PDF.");

        return nome;
    }
}
