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

    var leitura = leitor.Ler(escolhida);
    atual.Definir(leitura.Pasta);
    return Results.Ok(new
    {
        cancelado = false,
        pasta = leitura.Pasta,
        paginas = leitura.Paginas,
        avisos = leitura.Avisos
    });
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
        if (!atual.TentarObterPasta(out var pasta))
            throw new PedidoInvalidoException("Escolha uma pasta antes de unificar.");

        var resultado = unificador.Unificar(pasta, pedido?.Paginas ?? []);
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

/// <summary>
/// Recebe a ordem das páginas enviada pela página.
/// </summary>
/// <param name="Paginas">As páginas, na ordem do arquivo final.</param>
internal sealed record PedidoHttp(IReadOnlyList<PaginaPedida>? Paginas);
