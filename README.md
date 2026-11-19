# Folio

Aplicação local em .NET para juntar vários PDFs de uma pasta em um único arquivo.

Você escolhe a pasta no Windows, vê cada arquivo uma vez e reordena as páginas. O resultado é gravado na mesma pasta como `unificado.pdf`. Se esse nome já existir, o próximo arquivo sai como `unificado-2.pdf`, `unificado-3.pdf` e assim por diante.

O app abre no navegador, em `http://127.0.0.1:5088`, e só escuta na própria máquina. A leitura e a gravação dos PDFs são feitas em C#, com a biblioteca [PDFsharp](https://docs.pdfsharp.net/) (licença MIT).

## O que dá para fazer

- Escolher uma pasta do computador.
- Ler só os `.pdf` dessa pasta, sem entrar em subpastas.
- Ver as páginas de cada arquivo e mudar a ordem arrastando a linha ou usando as setas.
- Gerar um PDF único sem sobrescrever um arquivo que já existe.
- Abrir a pasta no Explorer no arquivo que acabou de ser gravado.

## Requisitos

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download)

## Como executar

Na raiz do repositório:

```powershell
dotnet run --project src/PdfTool.Web
```

O navegador abre em `http://127.0.0.1:5088`. Para subir sem abrir o navegador:

```powershell
$env:PDFTOOL_SEM_NAVEGADOR = "1"
dotnet run --project src/PdfTool.Web
```

## Testes

```powershell
dotnet test
```

## Estrutura

- `src/PdfTool.Core` — leitura da pasta e unificação dos PDFs
- `src/PdfTool.Web` — interface web e a janela de escolha da pasta
- `tests/PdfTool.Tests` — testes da ordem das páginas, do nome do arquivo de saída e da leitura da pasta
