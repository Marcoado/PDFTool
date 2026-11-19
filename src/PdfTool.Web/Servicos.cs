using System.Runtime.Versioning;
using System.Windows.Forms;
using PdfTool.Core;

namespace PdfTool.Web;

/// <summary>
/// Guarda a pasta escolhida nesta execução e o último PDF gerado.
/// </summary>
public sealed class PastaAtual
{
    private readonly Lock _trava = new();
    private string? _caminho;
    private string? _ultimoArquivo;
    private List<ItemSelecionado> _itens = [];

    /// <summary>
    /// Obtém a pasta escolhida, ou <see langword="null"/> se o usuário ainda não escolheu.
    /// </summary>
    public string? Caminho
    {
        get
        {
            lock (_trava)
                return _caminho;
        }
    }

    /// <summary>
    /// Define a pasta de saída e os arquivos escolhidos.
    /// </summary>
    /// <param name="pastaSaida">A pasta onde o PDF unificado será gravado.</param>
    /// <param name="itens">Os arquivos válidos, na ordem inicial.</param>
    public void DefinirSelecao(string pastaSaida, IReadOnlyList<ItemSelecionado> itens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pastaSaida);
        ArgumentNullException.ThrowIfNull(itens);
        lock (_trava)
        {
            _caminho = pastaSaida;
            _itens = itens.ToList();
            _ultimoArquivo = null;
        }
    }

    /// <summary>
    /// Tenta obter a seleção atual.
    /// </summary>
    /// <param name="pastaSaida">Quando este método retorna, contém a pasta de saída, se houver.</param>
    /// <param name="itens">Quando este método retorna, contém os arquivos escolhidos.</param>
    /// <returns><see langword="true"/> se há uma pasta; caso contrário, <see langword="false"/>.</returns>
    public bool TentarObterSelecao(out string pastaSaida, out IReadOnlyList<ItemSelecionado> itens)
    {
        lock (_trava)
        {
            if (_caminho is null)
            {
                pastaSaida = string.Empty;
                itens = [];
                return false;
            }

            pastaSaida = _caminho;
            itens = _itens.ToArray();
            return true;
        }
    }

    /// <summary>
    /// Define a pasta de trabalho e descarta o atalho do arquivo gerado antes.
    /// </summary>
    /// <param name="caminho">O caminho completo da pasta.</param>
    public void Definir(string caminho)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caminho);
        lock (_trava)
        {
            _caminho = caminho;
            _ultimoArquivo = null;
        }
    }

    /// <summary>
    /// Tenta obter a pasta escolhida.
    /// </summary>
    /// <param name="caminho">Quando este método retorna, contém a pasta, se houver.</param>
    /// <returns><see langword="true"/> se há uma pasta; caso contrário, <see langword="false"/>.</returns>
    public bool TentarObterPasta(out string caminho)
    {
        lock (_trava)
        {
            if (_caminho is null)
            {
                caminho = string.Empty;
                return false;
            }

            caminho = _caminho;
            return true;
        }
    }

    /// <summary>
    /// Guarda o caminho do último PDF unificado.
    /// </summary>
    /// <param name="caminho">O caminho completo do arquivo gerado.</param>
    public void DefinirUltimoArquivo(string caminho)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caminho);
        lock (_trava)
            _ultimoArquivo = caminho;
    }

    /// <summary>
    /// Tenta obter o último PDF unificado.
    /// </summary>
    /// <param name="caminho">Quando este método retorna, contém o arquivo, se houver.</param>
    /// <returns><see langword="true"/> se há um arquivo; caso contrário, <see langword="false"/>.</returns>
    public bool TentarObterUltimoArquivo(out string caminho)
    {
        lock (_trava)
        {
            if (_ultimoArquivo is null)
            {
                caminho = string.Empty;
                return false;
            }

            caminho = _ultimoArquivo;
            return true;
        }
    }
}

/// <summary>
/// Guarda um PDF escolhido nesta execução.
/// </summary>
/// <param name="Id">O identificador usado pela página para reordenar.</param>
/// <param name="Caminho">O caminho completo do arquivo.</param>
/// <param name="Nome">O nome do arquivo, sem caminho.</param>
/// <param name="Paginas">A quantidade de páginas do arquivo.</param>
/// <param name="Pasta">A pasta em que o arquivo está.</param>
public sealed record ItemSelecionado(string Id, string Caminho, string Nome, int Paginas, string Pasta);

/// <summary>
/// Abre a janela do Windows para o usuário escolher uma pasta.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SeletorDePasta
{
    /// <summary>
    /// Mostra o seletor de pasta e espera o usuário confirmar ou cancelar.
    /// </summary>
    /// <param name="pastaInicial">A pasta já escolhida, usada como ponto de partida. Pode ser <see langword="null"/>.</param>
    /// <returns>O caminho escolhido, ou <see langword="null"/> se o usuário cancelar.</returns>
    /// <exception cref="FalhaDeOperacaoException">A janela de pasta não pôde ser aberta.</exception>
    public string? Escolher(string? pastaInicial)
    {
        string? escolhida = null;
        Exception? falha = null;
        var thread = new Thread(() =>
        {
            try
            {
                escolhida = MostrarDialogo(pastaInicial);
            }
            catch (Exception excecao)
            {
                falha = excecao;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name = "Seletor de pasta";
        thread.Start();
        thread.Join();

        if (falha is not null)
            throw new FalhaDeOperacaoException("Não foi possível abrir a janela de pasta do Windows.", falha);

        return string.IsNullOrWhiteSpace(escolhida) ? null : Path.GetFullPath(escolhida);
    }

    /// <summary>
    /// Mostra o seletor de arquivos PDF e espera o usuário confirmar ou cancelar.
    /// </summary>
    /// <param name="pastaInicial">A pasta já escolhida, usada como ponto de partida. Pode ser <see langword="null"/>.</param>
    /// <returns>Os caminhos escolhidos, ou <see langword="null"/> se o usuário cancelar.</returns>
    /// <exception cref="FalhaDeOperacaoException">A janela de arquivos não pôde ser aberta.</exception>
    public string[]? EscolherArquivos(string? pastaInicial)
    {
        string[]? escolhidos = null;
        Exception? falha = null;
        var thread = new Thread(() =>
        {
            try
            {
                escolhidos = MostrarArquivos(pastaInicial);
            }
            catch (Exception excecao)
            {
                falha = excecao;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name = "Seletor de arquivos";
        thread.Start();
        thread.Join();

        if (falha is not null)
            throw new FalhaDeOperacaoException("Não foi possível abrir a janela de arquivos do Windows.", falha);

        return escolhidos is null || escolhidos.Length == 0
            ? null
            : escolhidos.Select(Path.GetFullPath).ToArray();
    }

    private static string[]? MostrarArquivos(string? pastaInicial)
    {
        Application.EnableVisualStyles();

        using var dono = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(1, 1),
            TopMost = true
        };

        string[]? escolhidos = null;
        dono.Shown += (_, _) =>
        {
            dono.Activate();
            using var dialogo = new OpenFileDialog
            {
                Title = "Escolha os arquivos PDF",
                Filter = "PDF (*.pdf)|*.pdf",
                Multiselect = true,
                CheckFileExists = true
            };

            if (!string.IsNullOrWhiteSpace(pastaInicial) && Directory.Exists(pastaInicial))
                dialogo.InitialDirectory = pastaInicial;

            if (dialogo.ShowDialog(dono) == DialogResult.OK)
                escolhidos = dialogo.FileNames;

            dono.Close();
        };

        Application.Run(dono);
        return escolhidos;
    }

    private static string? MostrarDialogo(string? pastaInicial)
    {
        Application.EnableVisualStyles();

        using var dono = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(1, 1),
            TopMost = true
        };

        string? escolhida = null;
        dono.Shown += (_, _) =>
        {
            dono.Activate();
            using var dialogo = new FolderBrowserDialog
            {
                Description = "Escolha a pasta com os arquivos PDF",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (!string.IsNullOrWhiteSpace(pastaInicial) && Directory.Exists(pastaInicial))
                dialogo.SelectedPath = pastaInicial;

            if (dialogo.ShowDialog(dono) == DialogResult.OK)
                escolhida = dialogo.SelectedPath;

            dono.Close();
        };

        Application.Run(dono);
        return escolhida;
    }
}
