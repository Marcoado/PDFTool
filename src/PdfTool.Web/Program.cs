using Microsoft.AspNetCore.Diagnostics;
using PdfTool.Core;
using PdfTool.Web;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://127.0.0.1:5088");
builder.Services.AddSingleton<PastaAtual>();
builder.Services.AddSingleton<LeitorDePasta>();
builder.Services.AddSingleton<UnificadorDePdf>();
builder.Services.AddSingleton<SeletorDePasta>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PdfTool");

app.UseExceptionHandler(tratamento =>
{
    tratamento.Run(async contexto =>
    {
        var erro = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
        var pedidoInvalido = erro is PedidoInvalidoException;
        var mensagemConhecida = erro is PedidoInvalidoException or FalhaDeOperacaoException;
        contexto.Response.StatusCode = pedidoInvalido
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        logger.LogError(erro, "Falha ao processar {Metodo} {Caminho}", contexto.Request.Method, contexto.Request.Path);
        await contexto.Response.WriteAsJsonAsync(new
        {
            mensagem = mensagemConhecida ? erro!.Message : "Não foi possível concluir a operação."
        });
    });
});

app.UseDefaultFiles();
app.UseStaticFiles();

var unificacao = new SemaphoreSlim(1, 1);

app.MapPost("/api/pasta", (SeletorDePasta seletor, LeitorDePasta leitor, PastaAtual atual) =>
{
    var escolhida = seletor.Escolher(atual.Caminho);
    if (escolhida is null)
        return Results.Ok(new { cancelado = true });

    var leitura = leitor.LerPasta(escolhida);
    return Results.Ok(GuardarSelecao(atual, leitura));
});

app.MapPost("/api/arquivos", (SeletorDePasta seletor, LeitorDePasta leitor, PastaAtual atual) =>
{
    var escolhidos = seletor.EscolherArquivos(atual.Caminho);
    if (escolhidos is null)
        return Results.Ok(new { cancelado = true });

    var pastaSaida = Path.GetDirectoryName(escolhidos[0]);
    if (string.IsNullOrWhiteSpace(pastaSaida))
        throw new PedidoInvalidoException("Não foi possível identificar a pasta dos arquivos.");

    var leitura = leitor.LerArquivos(escolhidos, pastaSaida);
    return Results.Ok(GuardarSelecao(atual, leitura));
});

app.MapPost("/api/unificar", (PedidoHttp? pedido, PastaAtual atual, UnificadorDePdf unificador) =>
{
    if (!unificacao.Wait(0))
    {
        return Results.Json(
            new { mensagem = "Já existe uma unificação em andamento." },
            statusCode: StatusCodes.Status409Conflict);
    }

    try
    {
        if (!atual.TentarObterSelecao(out var pasta, out var itens) || itens.Count == 0)
            throw new PedidoInvalidoException("Escolha uma pasta ou arquivos antes de unificar.");

        var ids = pedido?.Ids ?? [];
        if (ids.Count == 0)
            throw new PedidoInvalidoException("Inclua ao menos um arquivo.");

        var porId = itens.ToDictionary(item => item.Id, StringComparer.Ordinal);
        var caminhos = new List<string>(ids.Count);
        foreach (var id in ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !porId.TryGetValue(id, out var item))
                throw new PedidoInvalidoException("A lista tem um arquivo que não foi escolhido.");

            caminhos.Add(item.Caminho);
        }

        var resultado = unificador.UnificarCaminhos(pasta, caminhos);
        atual.DefinirUltimoArquivo(resultado.Caminho);
        return Results.Ok(resultado);
    }
    finally
    {
        unificacao.Release();
    }
});

app.MapPost("/api/revelar", (PastaAtual atual) =>
{
    if (!atual.TentarObterUltimoArquivo(out var caminho) || !File.Exists(caminho))
        throw new PedidoInvalidoException("Ainda não há um PDF unificado nesta sessão.");

    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = "explorer.exe",
        Arguments = $"/select,\"{caminho}\"",
        UseShellExecute = true
    });

    return Results.Ok(new { caminho });
});

app.MapFallbackToFile("index.html");

if (!string.Equals(Environment.GetEnvironmentVariable("PDFTOOL_SEM_NAVEGADOR"), "1", StringComparison.Ordinal))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://127.0.0.1:5088")
            {
                UseShellExecute = true
            });
        }
        catch (Exception excecao)
        {
            logger.LogWarning(excecao, "Não foi possível abrir o navegador.");
        }
    });
}

app.Run();

static object GuardarSelecao(PastaAtual atual, ResultadoDeArquivos leitura)
{
    var itens = leitura.Arquivos
        .Select(arquivo => new ItemSelecionado(
            Guid.NewGuid().ToString("N"),
            arquivo.Caminho,
            arquivo.Arquivo,
            arquivo.Paginas,
            arquivo.Pasta))
        .ToArray();

    atual.DefinirSelecao(leitura.PastaSaida, itens);
    return new
    {
        cancelado = false,
        pasta = leitura.PastaSaida,
        arquivos = itens.Select(item => new
        {
            id = item.Id,
            arquivo = item.Nome,
            paginas = item.Paginas,
            pasta = item.Pasta
        }),
        avisos = leitura.Avisos
    };
}

/// <summary>
/// Recebe a ordem dos arquivos enviada pela página.
/// </summary>
/// <param name="Ids">Os identificadores dos arquivos, na ordem do PDF final.</param>
internal sealed record PedidoHttp(IReadOnlyList<string>? Ids);
