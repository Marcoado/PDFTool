namespace PdfTool.Core;

/// <summary>
/// Confere se um arquivo está diretamente na pasta escolhida.
/// </summary>
internal static class Caminhos
{
    /// <summary>
    /// Obtém um valor que indica se o arquivo está na pasta, sem subpastas.
    /// </summary>
    /// <param name="pasta">A pasta escolhida.</param>
    /// <param name="caminhoCompleto">O caminho completo do arquivo.</param>
    /// <returns>
    /// <see langword="true"/> se o arquivo está nessa pasta; caso contrário, <see langword="false"/>.
    /// </returns>
    public static bool EhArquivoDaPasta(string pasta, string caminhoCompleto)
    {
        var raiz = Path.GetFullPath(pasta).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var diretorio = Path.GetDirectoryName(Path.GetFullPath(caminhoCompleto));
        return string.Equals(diretorio, raiz, StringComparison.OrdinalIgnoreCase);
    }
}
