const escolherBtn = document.querySelector("#escolher");
const arquivosBtn = document.querySelector("#arquivos");
const unificarBtn = document.querySelector("#unificar");
const revelarBtn = document.querySelector("#revelar");
const pastaEl = document.querySelector("#pasta");
const resumoEl = document.querySelector("#resumo");
const statusEl = document.querySelector("#status");
const avisosEl = document.querySelector("#avisos");
const listaAvisos = document.querySelector("#lista-avisos");
const vazioEl = document.querySelector("#vazio");
const listaEl = document.querySelector("#lista");

let arquivos = [];
let arrastando = null;
let ocupado = false;

escolherBtn.addEventListener("click", () => escolher("/api/pasta", "Aguardando a janela de pasta do Windows…"));
arquivosBtn.addEventListener("click", () => escolher("/api/arquivos", "Aguardando a janela de arquivos do Windows…"));
unificarBtn.addEventListener("click", unificar);
revelarBtn.addEventListener("click", revelar);

async function escolher(url, espera) {
  definirOcupacao(true, espera);
  try {
    const resposta = await fetch(url, { method: "POST" });
    const corpo = await lerJson(resposta);
    if (!resposta.ok) throw new Error(corpo.mensagem || "Não foi possível ler os PDFs.");
    if (corpo.cancelado) {
      statusEl.textContent = arquivos.length ? descreverLista() : "Seleção cancelada.";
      return;
    }

    arquivos = Array.isArray(corpo.arquivos) ? corpo.arquivos : [];
    pastaEl.textContent = corpo.pasta || "Seleção pronta";
    revelarBtn.hidden = true;
    mostrarAvisos(corpo.avisos);
    renderizar(null);
    statusEl.textContent = arquivos.length
      ? descreverLista()
      : "Nenhum PDF válido nessa seleção.";
  } catch (erro) {
    statusEl.textContent = erro instanceof Error ? erro.message : "Não foi possível ler os PDFs.";
  } finally {
    definirOcupacao(false);
  }
}

async function unificar() {
  if (!arquivos.length || ocupado) return;
  const total = arquivos.reduce((soma, item) => soma + item.paginas, 0);
  definirOcupacao(true, `Unindo ${total} página(s)…`);
  try {
    const resposta = await fetch("/api/unificar", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ ids: arquivos.map((item) => item.id) })
    });
    const corpo = await lerJson(resposta);
    if (!resposta.ok) throw new Error(corpo.mensagem || "Não foi possível unificar.");
    statusEl.textContent = `PDF gravado: ${corpo.arquivo}`;
    revelarBtn.hidden = false;
  } catch (erro) {
    statusEl.textContent = erro instanceof Error ? erro.message : "Não foi possível unificar.";
  } finally {
    definirOcupacao(false);
  }
}

async function revelar() {
  try {
    const resposta = await fetch("/api/revelar", { method: "POST" });
    const corpo = await lerJson(resposta);
    if (!resposta.ok) throw new Error(corpo.mensagem || "Não foi possível abrir a pasta.");
  } catch (erro) {
    statusEl.textContent = erro instanceof Error ? erro.message : "Não foi possível abrir a pasta.";
  }
}

function renderizar(foco) {
  listaEl.replaceChildren();
  const temArquivos = arquivos.length > 0;
  vazioEl.hidden = temArquivos;
  listaEl.hidden = !temArquivos;
  resumoEl.textContent = temArquivos
    ? `${descreverLista()} Use a posição, os botões ou arraste a linha.`
    : "Escolha a pasta inteira ou só os PDFs que entram no arquivo.";

  arquivos.forEach((item, indice) => listaEl.append(criarLinha(item, indice)));
  unificarBtn.disabled = !temArquivos || ocupado;

  if (foco !== null) {
    const alvo = listaEl.querySelector(`[data-indice="${foco}"]`);
    if (alvo instanceof HTMLElement) alvo.focus();
  }
}

function criarLinha(item, indice) {
  const li = document.createElement("li");
  li.className = "arquivo-linha";
  li.draggable = true;
  li.tabIndex = 0;
  li.dataset.indice = String(indice);
  li.style.setProperty("--i", String(Math.min(indice, 14)));
  li.setAttribute("aria-label", `${rotulo(item)}. ${textoPaginas(item.paginas)}`);

  const faixa = document.createElement("span");
  faixa.className = "tinta-arquivo";
  faixa.style.background = corDoArquivo(item.arquivo);

  const meta = document.createElement("div");
  meta.className = "meta";
  const nome = document.createElement("span");
  nome.className = "arquivo";
  nome.textContent = rotulo(item);
  const origem = document.createElement("span");
  origem.className = "origem";
  origem.textContent = textoPaginas(item.paginas);
  meta.append(nome, origem);

  const controles = document.createElement("div");
  controles.className = "controles";
  const posicao = document.createElement("label");
  posicao.className = "posicao";
  posicao.append("Posição ");
  const campo = document.createElement("input");
  campo.type = "number";
  campo.min = "1";
  campo.max = String(arquivos.length);
  campo.value = String(indice + 1);
  campo.setAttribute("aria-label", `Posição de ${item.arquivo}`);
  campo.addEventListener("change", () => {
    const destino = Number(campo.value) - 1;
    moverPara(indice, destino);
  });
  posicao.append(campo);
  controles.append(
    posicao,
    criarAcao("Topo", indice === 0, () => moverPara(indice, 0)),
    criarAcao("Subir", indice === 0, () => moverPara(indice, indice - 1)),
    criarAcao("Descer", indice === arquivos.length - 1, () => moverPara(indice, indice + 1)),
    criarAcao("Fim", indice === arquivos.length - 1, () => moverPara(indice, arquivos.length - 1))
  );

  li.append(faixa, meta, controles);
  ligarArraste(li, indice);
  li.addEventListener("keydown", (evento) => {
    if (evento.target instanceof HTMLInputElement) return;
    if (evento.key === "ArrowUp") {
      evento.preventDefault();
      moverPara(indice, indice - 1);
    } else if (evento.key === "ArrowDown") {
      evento.preventDefault();
      moverPara(indice, indice + 1);
    } else if (evento.key === "Home") {
      evento.preventDefault();
      moverPara(indice, 0);
    } else if (evento.key === "End") {
      evento.preventDefault();
      moverPara(indice, arquivos.length - 1);
    }
  });
  return li;
}

function criarAcao(rotuloBotao, desativada, aoClicar) {
  const botao = document.createElement("button");
  botao.type = "button";
  botao.className = "acao";
  botao.textContent = rotuloBotao;
  botao.disabled = desativada;
  botao.addEventListener("click", aoClicar);
  return botao;
}

function ligarArraste(li, indice) {
  li.addEventListener("dragstart", (evento) => {
    if (evento.target instanceof Element && evento.target.closest("button, input, label")) {
      evento.preventDefault();
      return;
    }
    arrastando = indice;
    li.classList.add("is-dragging");
    evento.dataTransfer.effectAllowed = "move";
    evento.dataTransfer.setData("text/plain", String(indice));
  });
  li.addEventListener("dragend", () => {
    li.classList.remove("is-dragging");
    arrastando = null;
  });
  li.addEventListener("dragover", (evento) => {
    evento.preventDefault();
    evento.dataTransfer.dropEffect = "move";
    li.classList.add("is-alvo");
  });
  li.addEventListener("dragleave", () => li.classList.remove("is-alvo"));
  li.addEventListener("drop", (evento) => {
    evento.preventDefault();
    li.classList.remove("is-alvo");
    const de = arrastando ?? Number(evento.dataTransfer.getData("text/plain"));
    moverPara(de, indice);
  });
}

function moverPara(de, para) {
  if (Number.isNaN(de) || Number.isNaN(para)) return;
  const destino = Math.max(0, Math.min(arquivos.length - 1, para));
  if (de < 0 || de >= arquivos.length || de === destino) {
    renderizar(de);
    return;
  }
  const [item] = arquivos.splice(de, 1);
  arquivos.splice(destino, 0, item);
  renderizar(destino);
  statusEl.textContent = descreverLista();
}

function textoPaginas(quantidade) {
  return quantidade === 1 ? "1 página" : `contém ${quantidade} páginas`;
}

function rotulo(item) {
  const repetido = arquivos.filter((outro) => outro.arquivo === item.arquivo).length > 1;
  if (!repetido) return item.arquivo;
  const pasta = item.pasta ? item.pasta.split(/[/\\]/).filter(Boolean).pop() : "";
  return pasta ? `${item.arquivo} (${pasta})` : item.arquivo;
}

function descreverLista() {
  const paginas = arquivos.reduce((soma, item) => soma + item.paginas, 0);
  const rotuloArquivo = arquivos.length === 1 ? "arquivo" : "arquivos";
  const rotuloPagina = paginas === 1 ? "página" : "páginas";
  return `${arquivos.length} ${rotuloArquivo}, ${paginas} ${rotuloPagina}.`;
}

function mostrarAvisos(avisos) {
  listaAvisos.replaceChildren();
  const itens = Array.isArray(avisos) ? avisos : [];
  avisosEl.hidden = itens.length === 0;
  itens.forEach((texto) => {
    const li = document.createElement("li");
    li.textContent = texto;
    listaAvisos.append(li);
  });
}

function definirOcupacao(ativo, mensagem) {
  ocupado = ativo;
  escolherBtn.disabled = ativo;
  arquivosBtn.disabled = ativo;
  unificarBtn.disabled = ativo || arquivos.length === 0;
  if (mensagem) statusEl.textContent = mensagem;
}

function corDoArquivo(nome) {
  let hash = 0;
  for (let i = 0; i < nome.length; i += 1)
    hash = (hash * 33 + nome.charCodeAt(i)) >>> 0;
  return `hsl(${hash % 360} 42% 34%)`;
}

async function lerJson(resposta) {
  const texto = await resposta.text();
  if (!texto) return {};
  try {
    return JSON.parse(texto);
  } catch {
    return { mensagem: "A resposta do aplicativo não pôde ser lida." };
  }
}
