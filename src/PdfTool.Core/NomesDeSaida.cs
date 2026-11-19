namespace PdfTool.Core;

/// <summary>
/// Escolhe um nome livre para o PDF unificado.
/// </summary>
public static class NomesDeSaida
{
    /// <summary>
    /// Obtém o próximo caminho <c>unificado.pdf</c> ou <c>unificado-N.pdf</c> que ainda não existe.
    /// </summary>
    /// <param name="pasta">A pasta onde o arquivo será gravado.</param>
    /// <returns>O caminho completo do arquivo a gravar.</returns>
    /// <exception cref="PedidoInvalidoException">A pasta não existe.</exception>
    /// <exception cref="FalhaDeOperacaoException">Não há nome livre até <c>unificado-9999.pdf</c>.</exception>
    public static string ProximoUnificado(string pasta)
    {
        var raiz = Path.GetFullPath(pasta);
        if (!Directory.Exists(raiz))
            throw new PedidoInvalidoException("A pasta não existe.");

        var primeiro = Path.Combine(raiz, "unificado.pdf");
        if (!File.Exists(primeiro))
            return primeiro;

        for (var numero = 2; numero <= 9999; numero++)
        {
            var candidato = Path.Combine(raiz, $"unificado-{numero}.pdf");
            if (!File.Exists(candidato))
                return candidato;
        }

        throw new FalhaDeOperacaoException(
            "Não há nome livre para o PDF unificado nesta pasta.",
            new IOException("Os nomes de unificado.pdf até unificado-9999.pdf já existem."));
    }
}
