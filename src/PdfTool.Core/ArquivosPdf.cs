using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace PdfTool.Core;

/// <summary>
/// Lista os PDFs de uma pasta, uma vez cada.
/// </summary>
internal static class ArquivosPdf
{
    /// <summary>
    /// Obtém os PDFs da pasta, sem repetir o mesmo arquivo.
    /// </summary>
    /// <param name="pasta">A pasta escolhida.</param>
    /// <returns>Os caminhos completos, em ordem de nome.</returns>
    public static IReadOnlyList<string> Listar(string pasta)
    {
        return SemRepetidos(Directory.EnumerateFiles(pasta), ChaveUnica);
    }

    /// <summary>
    /// Descarta caminhos que apontam para o mesmo arquivo.
    /// </summary>
    /// <param name="caminhos">Os caminhos encontrados na pasta.</param>
    /// <param name="chave">A função que devolve a identidade única de cada caminho.</param>
    /// <returns>Os caminhos únicos, em ordem de nome.</returns>
    public static List<string> SemRepetidos(IEnumerable<string> caminhos, Func<string, string> chave)
    {
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lista = new List<string>();

        foreach (var caminho in caminhos)
        {
            if (!string.Equals(Path.GetExtension(caminho), ".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            var unico = chave(caminho);
            if (vistos.Add(unico))
                lista.Add(unico);
        }

        lista.Sort((esquerda, direita) => string.Compare(
            Path.GetFileName(esquerda),
            Path.GetFileName(direita),
            StringComparison.OrdinalIgnoreCase));
        return lista;
    }

    private static string ChaveUnica(string caminho)
    {
        var cheio = Path.GetFullPath(caminho);
        return OperatingSystem.IsWindows() ? CaminhoLongo(cheio) : cheio;
    }

    [SupportedOSPlatform("windows")]
    private static string CaminhoLongo(string caminho)
    {
        var buffer = new StringBuilder(260);
        var escrito = GetLongPathName(caminho, buffer, (uint)buffer.Capacity);
        if (escrito == 0)
            return caminho;

        if (escrito > buffer.Capacity)
        {
            buffer.Capacity = (int)escrito;
            escrito = GetLongPathName(caminho, buffer, (uint)buffer.Capacity);
            if (escrito == 0)
                return caminho;
        }

        return buffer.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLongPathName(string shortPath, StringBuilder buffer, uint bufferSize);
}
