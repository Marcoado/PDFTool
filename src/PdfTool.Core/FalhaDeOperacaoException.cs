namespace PdfTool.Core;

/// <summary>
/// Representa uma falha ao executar uma operação que o usuário já pediu corretamente.
/// </summary>
public sealed class FalhaDeOperacaoException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="FalhaDeOperacaoException"/>.
    /// </summary>
    /// <param name="mensagem">A descrição da falha.</param>
    /// <param name="interna">A exceção que originou a falha.</param>
    public FalhaDeOperacaoException(string mensagem, Exception interna)
        : base(mensagem, interna)
    {
    }
}
