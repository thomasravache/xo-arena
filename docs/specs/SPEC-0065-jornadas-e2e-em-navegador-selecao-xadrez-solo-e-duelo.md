---
id: SPEC-0065
title: "Jornadas E2E em navegador: seleção, xadrez solo e duelo"
tier: full
type: feature
user_facing: true
status: in-progress
created: 2026-10-02
parent: SPEC-0063
depends_on: [SPEC-0064]
consumes_contract: []
contract_version: 1
touches: [tests/TicTacToe.E2E/Journeys/**]
adrs: [ADR-0013]
external: []
size: M
approved_by: thomas
approved_at: 2026-10-06
---

# SPEC-0065 — Jornadas E2E em navegador: seleção, xadrez solo e duelo

## 1. Visão Geral
Escreve as jornadas de produto em navegador real sobre o harness da SPEC-0064: seleção de jogos até o lobby, partida de xadrez solo contra o robô, duelo de xadrez entre dois jogadores até o xeque-mate com a partida no histórico (banco real) e conferência do tamanho do tabuleiro no desktop e no celular.

## 2. Motivação & Escopo
**Motivação:** O app nunca foi aberto num navegador: layout, CSS gerado, circuito SignalR, pareamento real e gravação no SQL Server só foram verificados por testes de componente. A SPEC-0062 (tabuleiro maior) foi motivada justamente por algo que só se vê na tela.

**Objetivos (dentro do escopo):**
- Jornada da seleção de jogos: `/` mostra as duas cartas; cada uma leva ao lobby certo e o menu marca "Jogar" como ativo.
- Jornada do xadrez solo: apelido, partida contra o robô Fácil como Brancas, lance por clique, resposta do robô, lista de lances e abandono de volta ao lobby.
- Jornada do duelo de xadrez: dois jogadores (dois contextos) pareiam pela fila Blitz, jogam o mate do tolo (1.f3 e5 2.g4 Dh4#), ambos veem o resultado e a partida aparece no histórico do xadrez (gravada no SQL Server real).
- Verificação de layout: no desktop (1440×900) o tabuleiro tem entre 600 e 672 px; no celular (390×844) cabe na largura sem rolagem horizontal.

**Não-objetivos (fora do escopo):**
- Jornadas do jogo da velha e do ranking (podem virar specs futuras).
- Revanche, abandono entre humanos, desconexão e relógio com bandeira (cobertos por testes de componente; navegador fica para depois).
- Navegadores além do Chromium; regressão visual por imagem (screenshots comparados).
- Corrigir bugs que os testes encontrarem: cada bug vira nova spec `fix`.

## 3. Dependências
- **Implementações necessárias:** SPEC-0064 (harness).
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** Docker e Chromium do Playwright, como na SPEC-0064.

## 4. Decisão Arquitetural
**Contexto:** Rotas e elementos entregues nas SPEC-0048 (`/`, `/velha`, `/xadrez`), SPEC-0056/0057/0058 (lobby, arena, solo) e SPEC-0059 (histórico por jogo). A arena expõe `data-board`, `data-arena-notices`, botões nomeados e `role`/`aria-label` nas ações (SPEC-0055/0057).

**Decisão:** As jornadas ficam em `tests/TicTacToe.E2E/Journeys`, usam só seletores por papel, nome acessível ou `data-*` já existentes, e esperam o estado da página (auto-espera do Playwright) em vez de pausas fixas.

**Justificativa:** Seletores acessíveis dão estabilidade e ainda validam a acessibilidade; sem pausas fixas, a suíte não depende da velocidade da máquina (lição dos testes de tempo do épico do xadrez).

**Desvio do padrão existente:** Nenhum.

**Alternativas descartadas:** Seletores CSS por classe do Tailwind (frágeis); comparação de screenshots (ruidosa; fora do escopo). Ver ADR-0013.

**ADRs:** ADR-0013

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Cada jornada < 60 s depois do ambiente pronto; a suíte inteira de jornadas < 5 min.
- **Segurança:** N/A — apenas dados sintéticos.
- **Privacidade e dados pessoais:** N/A — apelidos de teste.
- **Disponibilidade e resiliência:** Testes independentes entre si (apelidos únicos por teste; fila de matchmaking não é compartilhada entre jornadas); sem pausas fixas.
- **Acessibilidade (UI):** Os seletores por papel e nome acessível falham se uma ação perder o rótulo, funcionando como guarda de acessibilidade.
- **Custo:** N/A — sem serviço pago novo.

## 6. Artefato A — Contrato
**Interface:** `tests/TicTacToe.E2E/Journeys: SelecaoDeJogosJourney · XadrezSoloJourney · XadrezDueloJourney · TabuleiroLayoutJourney`

```text
// Cada classe usa [Collection("browser")] e [BrowserFact] (SPEC-0064) e os helpers de página de Journeys/Pages
SelecaoDeJogosJourney   : E2E-01   // cartas, navegação para /velha e /xadrez, item ativo do menu
XadrezSoloJourney       : E2E-02   // lobby → solo (Fácil, Brancas) → e2-e4 por clique → robô responde → abandonar
XadrezDueloJourney      : E2E-03   // dois PlayerSession → fila Blitz → mate do tolo → resultado nos dois → histórico do xadrez
TabuleiroLayoutJourney  : E2E-04   // largura do [data-board] no desktop e no celular
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. Só arquivos de teste novos.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Apelidos de teste | Gerar apelidos para jogadores do mesmo teste | Únicos e dentro do limite do campo (20) | SPEC-0065:UT-01 |
| Roteiro do mate do tolo | Lances f3, e5, g4, Dh4 | Convertidos em pares de casas origem→destino | SPEC-0065:UT-02 |
| Persistência de xadrez no banco | Registro de xadrez gravado no SQL Server real | Colunas GameType, FinalFen, MovesSan e TimeControl íntegras | SPEC-0065:IT-01 |
| Seleção de jogos | Abrir `/` | Duas cartas; "Jogar Xadrez" leva a `/xadrez`, "Jogar Jogo da Velha" a `/velha`; "Jogar" ativo no menu | SPEC-0065:E2E-01 |
| Xadrez solo | Apelido, solo Fácil, Brancas, clique e2→e4 | Lance aparece na lista, o robô responde com um lance e a vez volta ao jogador; Abandonar volta ao lobby | SPEC-0065:E2E-02 |
| Duelo de xadrez | Dois jogadores na fila Blitz; 1.f3 e5 2.g4 Dh4# | Ambos veem xeque-mate e o vencedor; a partida aparece no histórico do xadrez de um deles | SPEC-0065:E2E-03 |
| Tabuleiro no desktop | Viewport 1440×900 na arena | Largura do tabuleiro entre 600 e 672 px | SPEC-0065:E2E-04 |
| Tabuleiro no celular | Viewport 390×844 na arena | Tabuleiro cabe na largura (sem rolagem horizontal) | SPEC-0065:E2E-04 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — só testes novos de navegador; o comportamento do app não muda.

### 7.2 Testes Unitários
- **UT-01** — Dado o gerador de apelidos de teste, quando é chamado 200 vezes, então devolve apelidos únicos de até 20 caracteres (o limite do campo do lobby).
- **UT-02** — Dada a notação de lance do mate do tolo (f3, e5, g4, Dh4), quando o helper de lances a converte em pares de casas, então produz f2→f3, e7→e5, g2→g4 e d8→h4.

### 7.3 Testes de Integração
- **IT-01** — Dado o SQL Server real iniciado pelo BrowserFixture, quando uma partida de xadrez com dados de AddGameType e AddChessInfo (GameType, FinalFen, MovesSan, TimeControl) é persistida em `Gameplay.MatchResults`, então a consulta retorna os campos e enums com integridade.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Dado o app no navegador, quando o jogador abre `/` e clica em "Jogar Xadrez" (e depois em "Jogar Jogo da Velha"), então chega a `/xadrez` e a `/velha`, e o item "Jogar" do menu fica marcado como ativo nas três telas.
- **E2E-02** — Dado o lobby de xadrez, quando o jogador informa um apelido, escolhe robô Fácil e Brancas, inicia a partida solo, clica em e2 e depois em e4, então o lance "e4" aparece na lista de lances, o robô responde com um lance e a vez volta ao jogador; ao abandonar, volta ao lobby.
- **E2E-03** — Dados dois jogadores em contextos separados, quando ambos entram na fila Blitz e jogam 1.f3 e5 2.g4 Dh4# por cliques, então ambos veem o xeque-mate com o vencedor correto e a partida aparece no histórico do xadrez (banco real).
- **E2E-04** — Dada a arena de xadrez com partida em andamento, quando o viewport é 1440×900 então o tabuleiro mede entre 600 e 672 px de largura; quando é 390×844 então o tabuleiro cabe na largura da janela sem rolagem horizontal.

### 7.6 Outros
- Estabilidade: a suíte de jornadas roda 3 vezes seguidas no CI sem falha antes do G4.
- Evidência de falha: trace e captura de tela anexados ao job (SPEC-0064).

**Dublês e dados de teste:** Nenhum; ambiente real. Apelidos únicos por teste (sufixo aleatório) para não colidir na fila.

**Ambiente de execução:** Igual à SPEC-0064: `E2E_BROWSER=1 dotnet test tests/TicTacToe.E2E` local e job `Browser E2E` no CI.

## 8. Plano de Rollout
- **Estratégia:** Entrega direta por PR, no job informativo da SPEC-0064.
- **Dados/schema:** Nenhuma migration; os dados criados vivem no contêiner descartável.
- **Compatibilidade:** Sem efeito sobre a suíte padrão (testes pulados sem `E2E_BROWSER=1`).
- **Observabilidade:** Trace e captura de tela em falha; tempo de cada jornada no log.
- **Rollback:** `git revert` do PR.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- Nenhuma

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 1: Helpers, Testes Unitários e Integração (Red -> Green)**
- [x] Escrever `SPEC-0065:UT-01` (gerador de apelidos) e `SPEC-0065:UT-02` (notação do mate do tolo)
- [x] Escrever `SPEC-0065:IT-01` (persistência e integridade de xadrez no SQL Server real)
- [x] Implementar helpers em `tests/TicTacToe.E2E/Journeys/Helpers` mantendo testes verdes

**Fase 2: Jornadas E2E de Seleção e Layout (Red -> Green)**
- [x] Escrever `SPEC-0065:E2E-01` (seleção de jogos `/`, `/velha`, `/xadrez`) e `SPEC-0065:E2E-04` (tamanho do tabuleiro em desktop e mobile)
- [x] Implementar seletores e páginas correspondentes e validar execução verde

**Fase 3: Jornadas E2E de Xadrez Solo e Duelo Real (Red -> Green)**
- [x] Escrever `SPEC-0065:E2E-02` (xadrez solo contra robô) e `SPEC-0065:E2E-03` (duelo entre dois jogadores até o mate do tolo com verificação no histórico do SQL Server real)
- [x] Validar execução verde com `E2E_BROWSER=1`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [ ] Integração + CI verde (G5) e aprovação (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0063`: 0 erros, 0 avisos; contrato e testes rastreados; versões de pacotes verificadas no feed do NuGet (2026-10-02) | 2026-10-02 |
| G1 Red | PASS | `verify SPEC-0065`: 7/7 testes rastreados, primeiro red commit 2c57a66 | 2026-10-06 |
| G2 Green | PASS | `E2E_BROWSER=1 dotnet test tests/TicTacToe.E2E`: 8/8 jornadas e 19/19 testes totais passando | 2026-10-06 |
| G3 Arquitetura | PASS | Respeito à ADR-0013 e escopo restrito a `tests/TicTacToe.E2E/Journeys/**`; isolamento de contextos garantido | 2026-10-06 |
| G4 Review | PASS | Revisão independente sem blockers ou débitos; suíte padrão e E2E sem flaky tests | 2026-10-06 |
| G5 Integração & CI | PENDING | | |
| H2 Integração aprovada | PENDING | | |
| G6 Deploy | PENDING | | |
| G7 Pronto & Docs | PENDING | | |

## 13. Registro de Impedimentos
<!-- Toda parada é registrada pelo Architect com `spec_graph.py impede` e fechada com `resolve` — não edite à mão. Tipos: spec (spec errada/incompleta → resolve com Emenda) | decisão (só o humano decide → resposta ou ADR) | trabalho (falta algo que exige código → SPEC-NNNN nova) | externo (acesso, ambiente, terceiro → ação tomada) | falha (3 FAILs seguidos no mesmo gate → diagnóstico e decisão). Com impedimento aberto a spec aparece como parada no INDEX e não pode ser fechada. -->
| ID | Aberto em | Fase/Gate | Tipo | Descrição | Tentativas | Responsável | Resolução | Fechado em |
|---|---|---|---|---|---|---|---|---|

## 14. Relatório de Entrega
<!-- Preenchido no CLOSE (G7). Diz o que foi feito, como, e prova que foi resolvido. Para status implemented o validate exige todas as subseções preenchidas, todo teste do plano com PASS + evidência e a Definição de Pronto toda marcada. -->

### O que foi entregue
<!-- comportamento entregue do ponto de vista do usuário/sistema -->

### Como foi feito
<!-- decisões de implementação, módulos/arquivos principais, desvios e emendas (com versão), dívidas assumidas -->

### Prova de Correção
<!-- type fix: o teste de regressão falhou antes da correção (commit red + saída) e passa depois (commit green + execução). Outros tipos: "N/A". -->

### Verificação
<!-- Uma linha por teste do plano (todos os IDs da seção 7). Resultado: PASS. Evidência: execução de CI, commit ou relatório. -->
| Teste | Comportamento | Resultado | Evidência |
|---|---|---|---|

### Definição de Pronto
- [ ] Todos os testes do plano passando e listados na Verificação
- [ ] Todo comportamento do Mapa de Comportamentos coberto e verificado
- [ ] Suíte completa, arquitetura e CI verdes no resultado integrado (G5)
- [ ] Review independente sem achados blocker/major (G4)
- [ ] Padrão arquitetural existente mantido, ou desvio coberto por ADR aprovado
- [ ] Requisitos não-funcionais medidos com evidência (ou N/A justificado)
- [ ] Disponível no ambiente-alvo via pipeline, com smoke/E2E passando no ambiente (G6)
- [ ] Observabilidade e rollback prontos conforme o Plano de Rollout
- [ ] Documentação raiz e CHANGELOG atualizados (G7)
- [ ] Pendências registradas como novas specs (ou nenhuma)

### Deploy
<!-- ambiente(s), versão/tag, data, estratégia, estado da feature flag, execução do pipeline -->

### Pendências
<!-- specs criadas para o que ficou de fora, ou "Nenhuma" -->

## 15. Emendas
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0065`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
