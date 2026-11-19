namespace PdfTool.Core;

/// <summary>
/// Identifica uma página lida de um PDF da pasta escolhida.
/// </summary>
/// <param name="Arquivo">O nome do arquivo, sem caminho.</param>
/// <param name="Pagina">O número da página, começando em 1.</param>
/// <param name="TotalNoArquivo">A quantidade de páginas desse arquivo.</param>
public sealed record PaginaDeOrigem(string Arquivo, int Pagina, int TotalNoArquivo);

/// <summary>
/// Identifica uma página que deve entrar no PDF unificado.
/// </summary>
/// <param name="Arquivo">O nome do arquivo, sem caminho.</param>
/// <param name="Pagina">O número da página, começando em 1.</param>
public sealed record PaginaPedida(string Arquivo, int Pagina);

/// <summary>
/// Reúne as páginas encontradas em uma pasta e os arquivos que não puderam ser lidos.
/// </summary>
/// <param name="Pasta">O caminho completo da pasta.</param>
/// <param name="Paginas">As páginas, na ordem padrão de leitura.</param>
/// <param name="Avisos">As mensagens dos arquivos ignorados.</param>
public sealed record ResultadoDeLeitura(
    string Pasta,
    IReadOnlyList<PaginaDeOrigem> Paginas,
    IReadOnlyList<string> Avisos);

/// <summary>
/// Descreve o PDF gerado pela unificação.
/// </summary>
/// <param name="Arquivo">O nome do arquivo gerado.</param>
/// <param name="Caminho">O caminho completo do arquivo gerado.</param>
/// <param name="TotalPaginas">A quantidade de páginas gravadas.</param>
public sealed record ResultadoDeUnificacao(string Arquivo, string Caminho, int TotalPaginas);
