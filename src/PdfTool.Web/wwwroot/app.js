const escolherBtn = document.querySelector("#escolher");
const unificarBtn = document.querySelector("#unificar");
const revelarBtn = document.querySelector("#revelar");
const pastaEl = document.querySelector("#pasta");
const resumoEl = document.querySelector("#resumo");
const statusEl = document.querySelector("#status");
const avisosEl = document.querySelector("#avisos");
const listaAvisos = document.querySelector("#lista-avisos");
const vazioEl = document.querySelector("#vazio");
const listaEl = document.querySelector("#lista");

let paginas = [];
let arrastando = null;
let ocupado = false;

escolherBtn.addEventListener("click", escolherPasta);
unificarBtn.addEventListener("click", unificar);
revelarBtn.addEventListener("click", revelar);

async function escolherPasta() {
  definirOcupacao(true, "Aguardando a janela de pasta do Windows…");
  try {
    const resposta = await fetch("/api/pasta", { method: "POST" });
    const corpo = await lerJson(resposta);
    if (!resposta.ok) throw new Error(corpo.mensagem || "Não foi possível ler a pasta.");
    if (corpo.cancelado) {
      statusEl.textContent = paginas.length
        ? descreverLista()
        : "Seleção cancelada.";
      return;
    }

    paginas = Array.isArray(corpo.paginas) ? corpo.paginas : [];
    pastaEl.textContent = corpo.pasta || "Pasta escolhida";
    revelarBtn.hidden = true;
    mostrarAvisos(corpo.avisos);
    renderizar(null);
    statusEl.textContent = paginas.length
      ? descreverLista()
      : "Nenhum PDF nesta pasta.";
  } catch (erro) {
    statusEl.textContent = erro instanceof Error ? erro.message : "Não foi possível ler a pasta.";
  } finally {
    definirOcupacao(false);
  }
}

async function unificar() {
  if (!paginas.length || ocupado) return;
  definirOcupacao(true, `Unindo ${paginas.length} página(s)…`);
  try {
    const resposta = await fetch("/api/unificar", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        paginas: paginas.map((item) => ({ arquivo: item.arquivo, pagina: item.pagina }))
      })
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
  const temPaginas = paginas.length > 0;
  vazioEl.hidden = temPaginas;
  listaEl.hidden = !temPaginas;
  resumoEl.textContent = temPaginas
    ? descreverLista() + " Cada arquivo entra uma vez."
    : "Cada arquivo da pasta entra uma vez. Arraste as páginas para mudar a ordem do PDF.";

  agrupar().forEach((grupo, indiceGrupo) => {
    const bloco = document.createElement("li");
    bloco.className = "bloco";
    bloco.style.setProperty("--i", String(Math.min(indiceGrupo, 14)));

    if (grupo.itens.length === 1) {
      bloco.append(criarLinha(grupo.itens[0].item, grupo.itens[0].indice, true));
    } else {
      bloco.append(criarCabeca(grupo), criarPaginas(grupo));
    }

    listaEl.append(bloco);
  });

  unificarBtn.disabled = !temPaginas || ocupado;
  if (foco !== null) {
    const alvo = listaEl.querySelector(`[data-indice="${foco}"]`);
    if (alvo instanceof HTMLElement) alvo.focus();
  }
}

function agrupar() {
  const grupos = [];
  paginas.forEach((item, indice) => {
    const ultimo = grupos[grupos.length - 1];
    if (ultimo && ultimo.arquivo === item.arquivo)
      ultimo.itens.push({ item, indice });
    else
      grupos.push({ arquivo: item.arquivo, itens: [{ item, indice }] });
  });
  return grupos;
}

function criarCabeca(grupo) {
  const cabeca = document.createElement("div");
  cabeca.className = "cabeca";
  const faixa = document.createElement("span");
  faixa.className = "tinta-arquivo";
  faixa.style.background = corDoArquivo(grupo.arquivo);
  const meta = document.createElement("div");
  meta.className = "meta";
  const arquivo = document.createElement("span");
  arquivo.className = "arquivo";
  arquivo.textContent = grupo.arquivo;
  const origem = document.createElement("span");
  origem.className = "origem";
  origem.textContent = `${grupo.itens.length} páginas`;
  meta.append(arquivo, origem);
  cabeca.append(faixa, meta);
  return cabeca;
}

function criarPaginas(grupo) {
  const ol = document.createElement("ol");
  ol.className = "paginas";
  grupo.itens.forEach(({ item, indice }) => ol.append(criarLinha(item, indice, false)));
  return ol;
}

function criarLinha(item, indice, mostrarArquivo) {
  const li = document.createElement("li");
  li.className = "folha";
  li.draggable = true;
  li.tabIndex = 0;
  li.dataset.indice = String(indice);
  li.setAttribute("aria-label", mostrarArquivo
    ? `${item.arquivo}, 1 página`
    : `${item.arquivo}, página ${item.pagina}`);

  const faixa = document.createElement("span");
  faixa.className = "tinta-arquivo";
  faixa.style.background = corDoArquivo(item.arquivo);

  const meta = document.createElement("div");
  meta.className = "meta";
  const arquivo = document.createElement("span");
  arquivo.className = "arquivo";
  arquivo.textContent = mostrarArquivo ? item.arquivo : `página ${item.pagina}`;
  const origem = document.createElement("span");
  origem.className = "origem";
  origem.textContent = mostrarArquivo ? "1 página" : `de ${item.totalNoArquivo}`;
  meta.append(arquivo, origem);

  const moverBox = document.createElement("div");
  moverBox.className = "mover";
  moverBox.append(
    criarSeta("Subir", "↑", indice === 0, () => mover(indice, indice - 1)),
    criarSeta("Descer", "↓", indice === paginas.length - 1, () => mover(indice, indice + 1))
  );

  const folio = document.createElement("span");
  folio.className = "folio";
  folio.textContent = String(indice + 1).padStart(2, "0");

  li.append(faixa, meta, moverBox, folio);
  ligarArraste(li, indice);
  li.addEventListener("keydown", (evento) => {
    if (evento.key === "ArrowUp") {
      evento.preventDefault();
      mover(indice, indice - 1);
    } else if (evento.key === "ArrowDown") {
      evento.preventDefault();
      mover(indice, indice + 1);
    }
  });
  return li;
}

function criarSeta(rotulo, texto, desativada, aoClicar) {
  const botao = document.createElement("button");
  botao.type = "button";
  botao.className = "seta";
  botao.textContent = texto;
  botao.setAttribute("aria-label", rotulo);
  botao.disabled = desativada;
  botao.addEventListener("click", aoClicar);
  return botao;
}

function ligarArraste(li, indice) {
  li.addEventListener("dragstart", (evento) => {
    if (evento.target instanceof Element && evento.target.closest("button")) {
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
    mover(de, indice);
  });
}

function mover(de, para) {
  if (para < 0 || para >= paginas.length || de === para || Number.isNaN(de)) return;
  const [item] = paginas.splice(de, 1);
  paginas.splice(para, 0, item);
  renderizar(para);
  statusEl.textContent = descreverLista();
}

function descreverLista() {
  const arquivos = new Set(paginas.map((item) => item.arquivo));
  const rotuloPagina = paginas.length === 1 ? "página" : "páginas";
  const rotuloArquivo = arquivos.size === 1 ? "arquivo" : "arquivos";
  return `${paginas.length} ${rotuloPagina} de ${arquivos.size} ${rotuloArquivo}.`;
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
  unificarBtn.disabled = ativo || paginas.length === 0;
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
