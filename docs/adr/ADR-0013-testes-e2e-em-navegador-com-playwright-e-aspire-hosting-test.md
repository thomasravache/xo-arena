---
id: ADR-0013
title: Testes E2E em navegador com Playwright e Aspire.Hosting.Testing
status: accepted
origin: decision
date: 2026-10-02
decision_makers: [thomas]
consulted: []
informed: []
supersedes:
superseded_by:
enforced_by: tests/TicTacToe.E2E/E2EHarnessTests.cs
---

# ADR-0013 — Testes E2E em navegador com Playwright e Aspire.Hosting.Testing

## Contexto e Problema
Os testes "E2E" do projeto são bUnit com EF InMemory (`tests/TicTacToe.Tests`): não carregam CSS, layout nem circuito SignalR, e nunca tocaram um SQL Server real (as migrations `AddGameType` e `AddChessInfo` foram escritas à mão). O `AppHost` (`src/TicTacToe/TicTacToe.AppHost/AppHost.cs`) já define o ambiente real: SQL Server em contêiner e o `webfrontend` com `WaitFor(sql)`; o app aplica `MigrateAsync()` ao subir (`Program.cs`). Como subir esse ambiente e controlar um navegador de forma reproduzível, local e no CI?

## Direcionadores da Decisão
- O ambiente de teste não pode divergir do real (mesmo SQL Server, mesma ligação por referência do Aspire).
- Reprodutível no CI (`ubuntu-latest`, com Docker) e executável localmente (Docker Desktop).
- A suíte padrão (`dotnet test`) continua rápida e sem exigir Docker nem navegador.
- Dois jogadores simultâneos no mesmo teste (contextos de navegador isolados).
- Menos peças móveis: aproveitar o que a stack (Aspire 13.5.4, .NET 10) já oferece.

## Opções Consideradas
- **A. `Aspire.Hosting.Testing` + Playwright**: o teste sobe o próprio `AppHost` com `DistributedApplicationTestingBuilder`; o Playwright controla o Chromium.
- **B. `Testcontainers.MsSql` + Playwright**: o teste sobe só o SQL Server e inicia o app à parte (processo ou fábrica customizada) apontando para a connection string.
- **C. Playwright contra um ambiente levantado à mão (`aspire run`)**: o teste só abre um navegador numa URL.

## Resultado da Decisão
**Opção escolhida:** **A**, porque reaproveita a definição do ambiente que já existe no `AppHost` (contêiner, variáveis, ordem de subida), evitando duplicá-la e deixá-la divergir; o `webfrontend` saudável implica SQL Server pronto e migrations aplicadas. Pacotes: `Aspire.Hosting.Testing` 13.5.4 (mesma versão do `AppHost`) e `Microsoft.Playwright` 1.63.0 (versões estáveis verificadas em 2026-10-02). Opt-in por `E2E_BROWSER=1` e job de CI separado, informativo no início.

### Consequências
- **Boa**, porque o ambiente de teste é o mesmo do desenvolvimento e qualquer mudança no `AppHost` vale para os testes.
- **Boa**, porque as migrations escritas à mão passam, pela primeira vez, por um SQL Server real.
- **Boa**, porque a suíte padrão não muda (testes pulados sem opt-in).
- **Ruim**, porque os testes dependem de Docker e do download do Chromium (~150 MB, com cache no CI) e são mais lentos que os de componente.
- **Ruim**, porque em Apple Silicon a imagem do SQL Server roda sob emulação (mais lenta); o CI amd64 é a referência.
- **Ruim**, porque o teste fica acoplado ao `AppHost` (nomes de recursos como `webfrontend`); mitigado por constantes no fixture.

### Confirmação (G3)
`tests/TicTacToe.E2E/E2EHarnessTests.cs` (SPEC-0064) verifica que nenhum projeto de produção referencia o projeto de E2E e que o E2E referencia apenas o `AppHost`; `BrowserFact` pulado sem `E2E_BROWSER=1` (SPEC-0064:UT-01) garante que a suíte padrão não exige Docker.

## Prós e Contras das Opções
| Critério (peso) | A. Aspire.Hosting.Testing | B. Testcontainers.MsSql | C. Ambiente manual |
|---|---|---|---|
| Fidelidade ao ambiente real (alto) | Total (mesmo `AppHost`) | Parcial (duplica a configuração) | Total, mas não controlada |
| Reprodutível no CI (alto) | Sim | Sim | Não |
| Esforço de manter (médio) | Baixo | Médio (subir o app à parte) | Baixo, porém manual |
| Peças móveis (médio) | Poucas (Aspire já em uso) | Mais um pacote e um ciclo de vida | Nenhuma |
| Independência do `AppHost` (baixo) | Acoplado | Independente | Acoplado |

## Mais Informações
- Specs: SPEC-0064 (harness), SPEC-0065 (jornadas); épico SPEC-0063.
- Versões estáveis conferidas no feed do NuGet em 2026-10-02: `Aspire.Hosting.Testing` 13.5.4 (a 13.6.0 já existe, mas o `AppHost` usa 13.5.4) e `Microsoft.Playwright` 1.63.0. Não consultei a documentação oficial do Playwright para .NET nesta sessão; a instalação por `Microsoft.Playwright.Program.Main(["install","chromium"])` é o procedimento documentado pelo projeto e deve ser confirmado pelo implementador.
- Padrão de opt-in por variável de ambiente já usado em `ChessPerftTests` (`CHESS_SLOW`).
