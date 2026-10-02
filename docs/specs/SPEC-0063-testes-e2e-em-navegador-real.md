---
id: SPEC-0063
title: Testes E2E em navegador real
tier: epic
type: feature
status: proposed
created: 2026-10-02
depends_on: []
adrs: [ADR-0013]
external: []
approved_by:
approved_at:
---

# SPEC-0063 — Testes E2E em navegador real (Épico)
## 1. Visão
Criar a camada de **testes E2E em navegador real** do projeto: o app roda de verdade (Blazor Server + SQL Server em contêiner, subidos pelo `AppHost` do Aspire) e o Playwright o controla num Chromium. Isso fecha as pendências deixadas pelos épicos anteriores: layout e CSS nunca vistos na tela, circuito SignalR, pareamento entre dois jogadores reais e migrations escritas à mão nunca aplicadas a um SQL Server.

## 2. Escopo
**Objetivos (dentro do escopo):**
- Harness reutilizável com `Aspire.Hosting.Testing` e `Microsoft.Playwright`, jogadores isolados, evidências de falha e job de CI separado e informativo (SPEC-0064, ADR-0013).
- Quatro jornadas de produto: seleção de jogos, xadrez solo, duelo de xadrez com histórico no banco real e tamanho do tabuleiro no desktop e no celular (SPEC-0065).

**Não-objetivos (fora do escopo):**
- Tornar o job de navegador obrigatório para o merge (só depois de estabilizar; decisão registrada nas questões abaixo).
- Jornadas do jogo da velha, ranking, revanche, desconexão e relógio em navegador.
- Outros navegadores, regressão visual por imagem, Lighthouse e axe automatizados.
- Qualquer mudança no código de produção.

## 3. Arquitetura Alvo
**Contexto:** `tests/TicTacToe.Tests` (xUnit + bUnit, EF InMemory) cobre lógica e componentes; o `AppHost` (`AddSqlServer` + `webfrontend`) define o ambiente real; o CI (`dotnet-ci`) roda a suíte padrão em `ubuntu-latest`.

```text
tests/TicTacToe.E2E (novo; opt-in E2E_BROWSER=1)
  BrowserFixture ──▶ DistributedApplicationTestingBuilder<AppHost> ──▶ SQL Server (contêiner) + webfrontend (migrations aplicadas)
        │
        └─▶ Playwright (Chromium) ──▶ PlayerSession (contexto isolado por jogador) ──▶ Journeys (SPEC-0065)
.github/workflows/browser-e2e.yml  (job "Browser E2E", informativo)
```

**Decisões (ADRs):** ADR-0013 — Playwright + `Aspire.Hosting.Testing`, com opt-in por variável de ambiente e job de CI separado.

**Regras de arquitetura a garantir (G3):**
- Nenhum projeto de produção referencia `tests/TicTacToe.E2E`; o projeto referencia apenas o `AppHost` — `E2EHarnessTests` (SPEC-0064) lê os `.csproj`.
- A suíte padrão (`dotnet test` da solução) não exige Docker nem navegador: os testes de navegador são pulados sem `E2E_BROWSER=1` — SPEC-0064:UT-01.

## 4. Decomposição
| Spec | Título | Tier | Tipo | Tamanho | Depende de | Consome contrato de |
|---|---|---|---|---|---|---|
| SPEC-0064 | Harness de testes E2E: Playwright e Aspire | full | foundation | M | — | — |
| SPEC-0065 | Jornadas E2E em navegador: seleção, xadrez solo e duelo | full | feature | M | SPEC-0064 | — |

## 5. Estratégia de Entrega
- **Ambientes:** só testes; sem ambiente remoto (`staging_url` vazio), G6 N/A como nos épicos anteriores.
- **Entrega por onda:** onda 1 = SPEC-0064; onda 2 = SPEC-0065. Cada uma vai para a `main` por PR.
- **Feature flags:** o opt-in `E2E_BROWSER=1` mantém a suíte padrão intacta.
- **Rollback:** `git revert` por PR; nada em produção depende dos testes.
- **Métricas de sucesso pós-release:** job `Browser E2E` verde 3 vezes seguidas; suíte padrão inalterada e sem Docker; qualquer bug real encontrado vira spec `fix` própria.

## 6. Riscos & Mitigações
- **Testes lentos e instáveis** — esperas pelo estado da página (auto-espera do Playwright), sem pausas fixas; seletores por papel e nome acessível; job informativo até estabilizar.
- **SQL Server emulado em Apple Silicon** — mais lento e sujeito a falha de subida local; o CI (amd64) é a referência; timeout explícito e mensagem clara no fixture.
- **Tempo de CI** — job separado em paralelo ao `build-and-test`; cache dos navegadores do Playwright.
- **Docker ausente na máquina do desenvolvedor** — testes pulados sem `E2E_BROWSER=1`; falha rápida com instrução quando o opt-in está ligado.
- **Bugs reais descobertos** (migrations, layout, pareamento) — não corrigidos nas specs de teste; cada um vira spec `fix`, e a jornada correspondente pode ser marcada como pulada com referência à spec até a correção.
- **Porta/HTTPS do `AppHost` no modo de teste** — o fixture usa o endpoint HTTP e o `Aspire.Hosting.Testing`; qualquer ajuste no `AppHost` seria uma spec própria.

## 7. Critérios de Aceite do Épico
- [ ] O app sobe em contêiner com SQL Server real e abre no Chromium — SPEC-0064:E2E-01
- [ ] Todas as migrations estão aplicadas no SQL Server real — SPEC-0064:IT-01
- [ ] Dois jogadores ficam isolados entre si — SPEC-0064:IT-02
- [ ] A seleção de jogos leva ao lobby certo no navegador — SPEC-0065:E2E-01
- [ ] O xadrez solo contra o robô funciona no navegador — SPEC-0065:E2E-02
- [ ] Dois jogadores jogam até o mate e a partida aparece no histórico do banco real — SPEC-0065:E2E-03
- [ ] O tabuleiro tem o tamanho esperado no desktop e cabe no celular — SPEC-0065:E2E-04

## 8. Questões em Aberto
- [x] Infra do teste: Aspire.Hosting.Testing ou Testcontainers.MsSql? — Aspire.Hosting.Testing (recomendado; reaproveita o `AppHost`) (thomas, 2026-10-02)
- [x] O job de navegador bloqueia o merge desde o início? — Não: informativo até estabilizar (recomendado) (thomas, 2026-10-02)
- [x] Escopo da primeira rodada? — As 4 jornadas: seleção, xadrez solo, duelo com histórico e tamanho do tabuleiro (thomas, 2026-10-02)

## 9. Aprovação (H1)
Uma aprovação humana cobre o épico e as specs filhas apresentadas junto com ele. Registrada no frontmatter (`approved_by`, `approved_at`) do épico e de cada filha.

## 10. Registro de Impedimentos
<!-- Problemas que envolvem várias filhas (conflito de integração da onda, CI quebrado, decisão transversal). Impedimento que trava uma filha específica vai na própria filha. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 11. Relatório de Entrega
<!-- Preenchido ao fechar o épico: o que foi entregue, como (ondas e deploys com versão/data), resultado dos critérios de aceite com os testes que os provam, métricas pós-release, pendências como novas specs. `spec_graph.py report SPEC-0063` ajuda a montar. -->

## 12. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda, aprovada pelo humano. -->
| Versão | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
