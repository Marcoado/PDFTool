namespace PdfTool.Core;

/// <summary>
/// Representa um pedido que não pode ser atendido com os dados recebidos.
/// </summary>
public sealed class PedidoInvalidoException : Exception
{
    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="PedidoInvalidoException"/>.
    /// </summary>
    /// <param name="mensagem">O motivo pelo qual o pedido foi recusado.</param>
    public PedidoInvalidoException(string mensagem)
        : base(mensagem)
    {
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="PedidoInvalidoException"/> com uma causa interna.
    /// </summary>
    /// <param name="mensagem">O motivo pelo qual o pedido foi recusado.</param>
    /// <param name="interna">A exceção que originou a recusa.</param>
    public PedidoInvalidoException(string mensagem, Exception interna)
        : base(mensagem, interna)
    {
    }
}
