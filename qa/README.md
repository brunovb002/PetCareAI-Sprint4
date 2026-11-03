# QA: PetCareAI (Sprint 4)

Compliance, Quality Assurance & Tests. Entrega da Sprint 4 do grupo PetCareAI (2TDSPX).

| Integrante | RM |
|---|---|
| Bruno Vinicius Barbosa | RM 566366 |
| Guilherme de Andrade Martini | RM 566087 |
| Nathan Gonçalves Pereira Mendes | RM 564666 |
| Raphael Gomes Mancera | RM 562279 |

## Links de entrega

| Item | Link |
|---|---|
| **Azure Boards: plano de testes manuais** | https://dev.azure.com/RM566366/Sprint%204%20%E2%80%93%20Azure%20DevOps/_boards/board |
| Consulta "Plano de testes - Sprint 4" (stories e casos de teste) | https://dev.azure.com/RM566366/web/qr.aspx?pguid=9328bfba-91c7-4fe1-a353-9811b89786f6&qid=b4d9ad57-cea3-4775-8a4d-f8896093973a |
| **Vídeo da automação dos testes (Insomnia)** | _adicionar o link do YouTube_ |
| API testada (Swagger) | https://app-petcareai-s4.azurewebsites.net/swagger |

O professor Douglas Andrade está na organização `RM566366` com acesso **Basic** e é **administrador** do projeto *Sprint 4 – Azure DevOps*.

---

## Parte A: Plano de testes manuais (Azure Boards)

Testes de validação no nível de sistema, executados contra a API publicada na Azure.

**Como está organizado no projeto *Sprint 4 – Azure DevOps*:**

- Iteração **Sprint 4** (13/10/2026 a 04/11/2026)
- Epic **#1 Release 1.0: PetCareAI (Sprint 4)**, o plano de release
- 4 **User Stories**, com os critérios de aceite escritos em **Gherkin** (Dado / Quando / Então) e as **tarefas** de desenvolvimento da sprint (já concluídas)
- Cada critério de aceite virou um **Test Case**, com o título começando pelo status code esperado. Cada caso tem:
  - **Objetivo**: descrição do caso
  - **Dados de entrada**: parâmetros `@` com valores definidos em *Parameter values* (dados controlados)
  - **Dados de saída**: o *Expected result* de cada passo
  - **Procedimento**: os passos (*Action*) do roteiro

| User Story | Casos de teste |
|---|---|
| #2 Autenticação de usuários com JWT | #17 `[201]` Cadastro de usuário válido · #18 `[409]` E-mail já cadastrado é recusado · #19 `[200]` Login com credenciais válidas · #20 `[401]` Login com senha incorreta |
| #6 Gestão de pets | #21 `[201]` Pet válido é cadastrado · #22 `[400]` Pet sem nome é recusado · #23 `[200]` Peso do pet é atualizado · #24 `[200]` Lista de pets filtrada e paginada · #25 `[204]` Pet é removido · #26 `[401]` Acesso sem token é negado · #27 `[403]` Tutor não altera pet de outro tutor |
| #10 Agendamento de consultas | #28 `[201]` Consulta é agendada para pet existente · #29 `[404]` Consulta para pet inexistente é recusada |
| #13 Avaliação de risco com IA (CLYVO AI) | #30 `[201]` Sintomas graves geram prioridade Alta · #31 `[400]` Espécie não suportada pela IA é recusada |

Os cenários Gherkin completos e a tabela de dados controlados também estão em [plano-de-testes.md](plano-de-testes.md).

---

## Parte B: Automação dos testes de API (Insomnia)

Como o sistema é uma API REST sem interface gráfica, a automação foi feita no **Insomnia**, com scripts **JavaScript** (*after-response scripts*) que validam cada resposta.

Coleção: [`insomnia/petcareai-insomnia.json`](insomnia/petcareai-insomnia.json), com **15 casos de teste automatizados** (sucesso e exceção):

| Pasta | Requisição | Validações |
|---|---|---|
| 0. Saúde da API | `[200] Health check (aquecimento)` | API e banco `Healthy` (também "acorda" o plano gratuito F1) |
| 1. Autenticação | `[201] Cadastro de usuário válido` | perfil Tutor, sem senha na resposta |
| | `[409] E-mail já cadastrado` | mensagem de e-mail duplicado |
| | `[401] Login com senha incorreta` | mensagem genérica de credenciais |
| | `[200] Login com credenciais válidas` | token Bearer, guardado no ambiente |
| 2. Pets | `[401] Listar pets sem token` | acesso negado |
| | `[201] Cadastrar pet` | id, dados e links HATEOAS; guarda o `petId` |
| | `[400] Cadastrar pet sem nome` | erro de validação no campo Nome |
| | `[200] Buscar pet por id` | retorna o pet cadastrado |
| | `[200] Atualizar pet` | idade e peso atualizados |
| | `[200] Listar pets filtrados e paginados` | só cachorros, até 2 por página, paginação |
| 3. Consultas | `[201] Agendar consulta` | consulta vinculada ao pet |
| | `[404] Agendar consulta para pet inexistente` | mensagem de pet não encontrado |
| 4. Remoção | `[204] Remover pet` | remoção |
| | `[404] Buscar pet removido` | o pet não existe mais |

Todas as requisições, menos o aquecimento, também validam o **desempenho**: a resposta precisa chegar em menos de `tempo_maximo_ms` (3000 ms, definido no ambiente).

### Como executar

1. Instalar o Insomnia: https://insomnia.rest/download
2. **Import** → selecionar `qa/insomnia/petcareai-insomnia.json`
3. Conferir o **Base Environment**: `base_url` aponta para a API na Azure, e `senha` e `tempo_maximo_ms` já vêm preenchidos
4. Clicar com o botão direito na coleção → **Run** (Collection Runner) → **Run**
5. Ver o resultado de cada teste na aba **Tests** de cada requisição e no resumo do Runner

Os dados são independentes a cada execução: o e-mail do tutor é gerado com a data e hora (`qa.<timestamp>@petcare.com`), e o pet criado é removido no final. Por isso a coleção pode rodar várias vezes seguidas, inclusive com várias iterações no Runner, como um teste de carga leve.
