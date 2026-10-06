---
id: SPEC-0064
title: "Harness de testes E2E: Playwright e Aspire"
tier: full
type: foundation
user_facing: false
status: in-progress
created: 2026-10-02
parent: SPEC-0063
depends_on: []
consumes_contract: []
contract_version: 1
touches: [TicTacToe.sln, .github/workflows/browser-e2e.yml, tests/TicTacToe.E2E/**, .gitignore]
adrs: [ADR-0013]
external: []
size: M
approved_by: thomas
approved_at: 2026-10-06
---

# SPEC-0064 — Harness de testes E2E: Playwright e Aspire

## 1. Visão Geral
Cria a base dos testes de navegador: um projeto `tests/TicTacToe.E2E` que sobe o `AppHost` do Aspire (SQL Server real em contêiner, migrations aplicadas e o app Blazor Server), abre o app num Chromium controlado pelo Playwright e oferece um fixture compartilhado, jogadores isolados (um contexto de navegador por jogador), evidências de falha (trace e captura de tela) e um job de CI próprio que não bloqueia o merge.

## 2. Motivação & Escopo
**Motivação:** Os testes "E2E" atuais são bUnit em memória: não carregam CSS, layout, circuito SignalR nem banco real. Por isso o tamanho do tabuleiro, as migrations `AddGameType`/`AddChessInfo` (escritas à mão) e os fluxos de F5/aba fechada nunca foram verificados. Sem uma base reutilizável, cada jornada em navegador teria de reinventar a subida do ambiente.

**Objetivos (dentro do escopo):**
- Projeto `tests/TicTacToe.E2E` (xUnit) na solução, referenciando o `AppHost`, com `Aspire.Hosting.Testing` e `Microsoft.Playwright` nas versões fixadas pelo ADR-0013.
- `BrowserFixture` (coleção xUnit compartilhada): sobe o `AppHost` uma vez por execução com `DistributedApplicationTestingBuilder`, espera o `webfrontend` ficar saudável (que implica o SQL Server pronto e as migrations aplicadas) e expõe a URL base HTTP.
- Instalação do Chromium do Playwright feita pelo próprio fixture (`Microsoft.Playwright.Program.Main(["install", "chromium"])`), sem depender de PowerShell.
- `PlayerSession`: um contexto de navegador isolado (armazenamento, cookies e circuito próprios) com viewport configurável, para simular dois jogadores no mesmo teste.
- Atributo `BrowserFact`: o teste só roda com `E2E_BROWSER=1`; sem isso é pulado (mesmo padrão do `CHESS_SLOW`), de modo que `dotnet test` da solução continua rápido e sem exigir Docker.
- Evidência de falha: trace do Playwright (`.zip`) e captura de tela salvos em `artifacts/e2e/` quando um teste falha.
- Job `Browser E2E` em `.github/workflows/browser-e2e.yml` (ubuntu-latest, Docker, cache dos navegadores, upload de `artifacts/e2e/` em falha), que roda a suíte com `E2E_BROWSER=1` e **não** é check obrigatório.
- Teste de fumaça do harness e verificação de que o banco real tem todas as migrations aplicadas.

**Não-objetivos (fora do escopo):**
- Jornadas de produto (seleção de jogos, xadrez, tamanho do tabuleiro): SPEC-0065.
- Mudar o `AppHost`, o app ou qualquer código de produção.
- Tornar o job de navegador obrigatório para o merge (decisão do épico: só depois de estabilizar).
- Navegadores além do Chromium; testes de carga, acessibilidade automatizada (axe) e Lighthouse.
- Persistência do SQL Server entre execuções (cada execução usa um banco novo).

## 3. Dependências
- **Implementações necessárias:** N/A — usa o `AppHost` e o app já entregues.
- **Contratos consumidos:** N/A
- **Pré-requisitos externos:** Docker em execução na máquina que roda a suíte (Docker Desktop local; o `ubuntu-latest` do GitHub Actions já tem). Em Apple Silicon a imagem do SQL Server roda sob emulação (mais lenta).

## 4. Decisão Arquitetural
**Contexto:** O `AppHost` (`src/TicTacToe/TicTacToe.AppHost/AppHost.cs`) já declara `AddSqlServer("sqlserver").AddDatabase("TicTacToeDb")` e liga o `webfrontend` com `WithReference(sql).WaitFor(sql)`; o app aplica `MigrateAsync()` na subida (`Program.cs`). Os testes existentes ficam em `tests/TicTacToe.Tests` (bUnit, EF InMemory) e usam o padrão de opt-in por variável de ambiente (`CHESS_SLOW`).

**Decisão:** Projeto novo `tests/TicTacToe.E2E` usando `Aspire.Hosting.Testing` para subir o `AppHost` real e `Microsoft.Playwright` para o navegador, com opt-in por `E2E_BROWSER=1` e job de CI separado e não obrigatório (ADR-0013).

**Justificativa:** Reaproveita a definição do ambiente que já existe (contêiner, variáveis, ordem de subida) em vez de duplicá-la, de modo que o ambiente de teste não diverge do real; o opt-in mantém a suíte padrão rápida e sem Docker.

**Desvio do padrão existente:** Nenhum: é um projeto de testes novo; não altera arquitetura nem código de produção.

**Alternativas descartadas:** `Testcontainers.MsSql` subindo só o SQL Server (duplica a configuração do `AppHost` e exige iniciar o app à parte); apontar o Playwright para um `aspire run` manual (não reprodutível no CI). Ver ADR-0013.

**ADRs:** ADR-0013

## 5. Requisitos Não-Funcionais
- **Desempenho e escala:** Subida do ambiente < 3 min no CI (SQL Server + app); suíte de navegador < 10 min no total; cada teste isolado por contexto de navegador, sem recriar o banco.
- **Segurança:** O SQL Server de teste usa a senha gerada pelo Aspire na execução, nunca gravada em arquivo nem em log; nenhum segredo novo no repositório; o job de CI só lê o repositório (`contents: read`).
- **Privacidade e dados pessoais:** N/A — apenas dados sintéticos criados pelos testes (apelidos de teste).
- **Disponibilidade e resiliência:** Testes de navegador não bloqueiam o merge; o fixture falha rápido (timeout explícito) com mensagem clara se o Docker não estiver disponível.
- **Acessibilidade (UI):** N/A — sem interface; os seletores dos testes usam papéis e nomes acessíveis, o que também protege a acessibilidade.
- **Custo:** Minutos extras de CI por PR (job separado); imagem do SQL Server baixada em cada execução do CI (cache de camadas do Docker quando disponível).

## 6. Artefato A — Contrato
**Interface:** `tests/TicTacToe.E2E: BrowserFixture · PlayerSession · BrowserFactAttribute · .github/workflows/browser-e2e.yml`

```text
// tests/TicTacToe.E2E (assembly de testes; não é referenciado por nenhum projeto de produção)
[CollectionDefinition("browser")] public sealed class BrowserCollection : ICollectionFixture<BrowserFixture> { }

public sealed class BrowserFixture : IAsyncLifetime
{
    public Uri BaseUri { get; }                                   // URL HTTP do webfrontend, já saudável
    public Task<string> GetSqlConnectionStringAsync();            // connection string do banco TicTacToeDb do contêiner
    public Task<PlayerSession> NewPlayerAsync(string name = "Jogador", ViewportSize? viewport = null);
    // InitializeAsync: instala o Chromium, sobe o AppHost (DistributedApplicationTestingBuilder) e espera "webfrontend" saudável
    // DisposeAsync: fecha o navegador e descarta o AppHost (contêiner removido)
}

public sealed class PlayerSession : IAsyncDisposable
{
    public IPage Page { get; }                                    // aba do jogador, com contexto isolado
    public string Name { get; }
    public Task GotoAsync(string path);                           // navega relativo a BaseUri e espera o circuito Blazor ficar interativo
}

[AttributeUsage(AttributeTargets.Method)] public sealed class BrowserFactAttribute : FactAttribute
    // Skip = "Defina E2E_BROWSER=1 para rodar os testes de navegador" quando a variável não for "1"

// Em falha: artifacts/e2e/<Classe>.<Teste>.zip (trace) e artifacts/e2e/<Classe>.<Teste>.png (captura de tela)

.github/workflows/browser-e2e.yml   // job "Browser E2E": setup-dotnet, cache de ~/.cache/ms-playwright, dotnet test tests/TicTacToe.E2E com E2E_BROWSER=1, upload de artifacts/e2e em falha; fora das proteções de branch
```

**Arquivos/módulos afetados:** ver `touches` no frontmatter. Projeto de testes novo, entrada na solução e um workflow; nenhum arquivo de produção muda.

### 6.1 Mapa de Comportamentos
| Cenário | Condição / Entrada | Resultado esperado | Testes |
|---|---|---|---|
| Fumaça do harness | `E2E_BROWSER=1`, Docker disponível | AppHost sobe, `webfrontend` saudável, a página `/` abre no Chromium com o título "Escolha seu jogo" | SPEC-0064:E2E-01 |
| Banco real migrado | Ambiente recém-subido | `__EFMigrationsHistory` no SQL Server contém todas as migrations do assembly de Gameplay | SPEC-0064:IT-01 |
| Jogadores isolados | Dois `PlayerSession` no mesmo teste | Armazenamento local e identidade de um não aparecem no outro | SPEC-0064:IT-02 |
| Opt-in desligado | `E2E_BROWSER` ausente ou diferente de `1` | `BrowserFact` é pulado com a mensagem de instrução; `dotnet test` da solução não exige Docker | SPEC-0064:UT-01 |
| Evidência de falha | Teste de navegador falha | Trace `.zip` e captura `.png` gravados em `artifacts/e2e/` com o nome do teste | SPEC-0064:UT-02 |
| Docker indisponível | `E2E_BROWSER=1` sem Docker | O fixture falha em até 60 s com mensagem que diz para iniciar o Docker (não trava a suíte) | SPEC-0064:UT-03 |

## 7. Artefato B — Plano de Testes (TDD)

### 7.1 Testes de Caracterização
N/A — projeto novo, sem código existente a caracterizar.

### 7.2 Testes Unitários
- **UT-01** — Dado o `BrowserFactAttribute`, quando `E2E_BROWSER` não é `1`, então `Skip` traz a mensagem de instrução; quando é `1`, `Skip` é nulo.
- **UT-02** — Dado o gerador de evidências, quando um teste falha, então grava `artifacts/e2e/<Classe>.<Teste>.zip` e `.png` (verificado com uma página simulada, sem subir o AppHost).
- **UT-03** — Dado o verificador de Docker do fixture, quando o daemon não responde, então lança erro com a mensagem "Inicie o Docker" em até 60 s (verificado com um verificador falso).

### 7.3 Testes de Integração
- **IT-01** — Dado o ambiente recém-subido pelo `BrowserFixture`, quando consulta `__EFMigrationsHistory` no SQL Server real, então todas as migrations do assembly `TicTacToe.Modules.Gameplay` estão aplicadas (inclui `AddGameType` e `AddChessInfo`).
- **IT-02** — Dados dois `PlayerSession` do mesmo fixture, quando um grava um apelido no armazenamento local, então o outro não o enxerga.

### 7.4 Testes de Contrato
N/A — sem contrato entre specs (o contrato desta spec é consumido pelas filhas seguintes por depends_on).

### 7.5 Testes E2E
- **E2E-01** — Dado o ambiente real (SQL Server em contêiner + app) e `E2E_BROWSER=1`, quando um jogador abre `/` no Chromium, então a seleção de jogos aparece com o título "Escolha seu jogo" e as duas cartas de jogo.

### 7.6 Outros
- Segurança: `dotnet list package --vulnerable` limpo para os pacotes novos; nenhuma credencial em log.
- Formatação: `dotnet format --verify-no-changes` cobre o projeto novo.
- CI: o job `Browser E2E` roda no PR da própria spec (informativo) e a execução verde é a evidência do G2 para o E2E-01 e o IT-01.

**Dublês e dados de teste:** Verificador de Docker e gerador de evidências com dublês simples; o restante usa o ambiente real. Dados: apelidos sintéticos criados pelo teste.

**Ambiente de execução:** Local: Docker Desktop + `E2E_BROWSER=1 dotnet test tests/TicTacToe.E2E`. CI: job `Browser E2E` (ubuntu-latest, Docker nativo). Em Apple Silicon o SQL Server roda sob emulação.

## 8. Plano de Rollout
- **Estratégia:** Entrega direta por PR; o job novo nasce informativo (fora das proteções de branch).
- **Dados/schema:** Nenhuma migration; o banco de teste é descartável (contêiner novo a cada execução).
- **Compatibilidade:** Sem efeito sobre a suíte padrão: os testes ficam pulados sem `E2E_BROWSER=1`, e o projeto compila junto com a solução.
- **Observabilidade:** Saídas do fixture (tempo de subida, versão do Chromium) no log do teste; trace e captura anexados ao job em falha.
- **Rollback:** `git revert` do PR; nada em produção depende do projeto.
- **Etapas de migração/coexistência:** N/A

## 9. Questões em Aberto
- Nenhuma

## 10. Aprovação (H1)
Registrada no frontmatter (`approved_by`, `approved_at`) somente depois que o humano responder "Aprovado". O arquiteto nunca aprova a própria spec.

## 11. Checklist de Implementação
<!-- Preenchido na fase PLAN, após a aprovação. Cada fase começa pelos testes. -->
**Fase 0: Scaffold do projeto e CI**
- [x] Criar projeto `tests/TicTacToe.E2E` na solução referenciando `AppHost`, pacotes `Aspire.Hosting.Testing` e `Microsoft.Playwright` e workflow `.github/workflows/browser-e2e.yml`

**Fase 1: Testes unitários e arquiteturais (Red)**
- [x] Escrever `SPEC-0064:UT-01` (`BrowserFactAttribute`), `SPEC-0064:UT-02` (evidências em falha) e `SPEC-0064:UT-03` (verificação do Docker) e teste de arquitetura garantindo dependências
- [x] Confirmar que falham pelo motivo certo com commit `test(...)`

**Fase 2: Implementação do Harness e Testes Unitários Verdes (Green)**
- [x] Implementar `BrowserFactAttribute`, `BrowserFixture`, `PlayerSession`, captura de artefatos
- [x] Suíte unitária e de arquitetura verde

**Fase 3: Testes de Integração e E2E Fumaça (Red -> Green)**
- [x] Escrever `SPEC-0064:IT-01` (migrations no banco real), `SPEC-0064:IT-02` (sessões isoladas) e `SPEC-0064:E2E-01` (fumaça da home)
- [x] Validar execução ponta a ponta com `E2E_BROWSER=1`

**Fase final: Integração, entrega e documentação**
- [x] Review independente (G4)
- [ ] Integração + CI verde (G5) e aprovação (H2)
- [ ] Relatório de Entrega, docs raiz e CHANGELOG (G7)

## 12. Registro de Gates
<!-- Status: PENDING | PASS | FAIL | N/A. PASS e N/A exigem evidência (comando + resultado, SHA, execução de CI, veredito). -->
| Gate | Status | Evidência | Data |
|---|---|---|---|
| G0 Spec | PASS | `spec_graph.py validate SPEC-0063`: 0 erros, 0 avisos; contrato e testes rastreados; versões de pacotes verificadas no feed do NuGet (2026-10-02) | 2026-10-02 |
| G1 Red | PASS | Red confirmado com commit test(...) c369c75; verify SPEC-0064 PASS (0 erros, 6/6 testes rastreados) | 2026-10-06 |
| G2 Green | PASS | `E2E_BROWSER=1 dotnet test`: 11 testes executados, 11 passaram (UT-01/02/03, IT-01/02, E2E-01 e arquitetura); sem E2E_BROWSER: 8 passaram, 3 pulados | 2026-10-06 |
| G3 Arquitetura | PASS | `E2EHarnessTests` verde: nenhum projeto de src referencia E2E; E2E referencia somente AppHost conforme ADR-0013 | 2026-10-06 |
| G4 Review | PASS | Diff revisado: apenas projeto de testes, sln e workflow; nenhum código de produção alterado; escopo 100% contido em touches | 2026-10-06 |
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
<!-- Mudança em spec aprovada: uma linha por emenda. Mudou o contrato? Incremente `contract_version` e rode `spec_graph.py impacted SPEC-0064`. -->
| Versão do contrato | Data | Mudança | Motivo | Specs impactadas | Aprovado por |
|---|---|---|---|---|---|
