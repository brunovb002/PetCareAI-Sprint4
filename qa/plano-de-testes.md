# Plano de testes manuais: PetCareAI (Sprint 4)

Testes de validação no nível de sistema, executados contra a API publicada na Azure:
**https://app-petcareai-s4.azurewebsites.net/swagger**

Organização no Azure DevOps (projeto **Sprint 4 – Azure DevOps**):

- **Release 1.0: PetCareAI** (Epic) → **User Stories** da Sprint 4, cada uma com os critérios de aceite em Gherkin
- Cada critério de aceite vira um **Test Case**, com o título começando pelo status code esperado
- **Test Plan "Sprint 4"** com uma suíte por User Story; os dados de teste ficam em parâmetros `@`

## Dados controlados

| Parâmetro | Valor |
|---|---|
| Tutor A | nome `Ana Tutora`, e-mail `ana.qa@petcare.com`, senha `Senha@123` |
| Tutor B | nome `Bia Tutora`, e-mail `bia.qa@petcare.com`, senha `Senha@123` |
| Pet A | `Thor`, Cachorro, Labrador, 11 anos, 28,5 kg |
| Pet para IA | `Fred`, Cachorro, Golden Retriever, 10 anos, 30,2 kg |
| Pet não suportado pela IA | `Kiwi`, Calopsita, SRD, 2 anos, 0,1 kg |
| Pet inexistente | id `999999` |

---

## US1: Autenticação de usuários

```gherkin
Cenário: [201] Cadastro de usuário válido
  Dado que não existe usuário com o e-mail ana.qa@petcare.com
  Quando envio o cadastro com nome, e-mail e senha válidos
  Então recebo 201 e os dados do usuário, sem a senha

Cenário: [409] E-mail já cadastrado
  Dado que o e-mail ana.qa@petcare.com já está cadastrado
  Quando tento cadastrar outro usuário com o mesmo e-mail
  Então recebo 409 com a mensagem "Já existe um usuário cadastrado com este e-mail."

Cenário: [200] Login com credenciais válidas
  Dado que o usuário ana.qa@petcare.com está cadastrado
  Quando faço login com e-mail e senha corretos
  Então recebo 200 com um token JWT do tipo Bearer

Cenário: [401] Login com senha incorreta
  Dado que o usuário ana.qa@petcare.com está cadastrado
  Quando faço login com a senha SenhaErrada1
  Então recebo 401 com a mensagem "E-mail ou senha inválidos."
```

## US2: Gestão de pets

```gherkin
Cenário: [201] Pet válido é cadastrado
  Dado que estou autenticado como a Tutora A
  Quando cadastro o pet Thor, Cachorro, Labrador, 11 anos, 28,5 kg
  Então recebo 201 com o id do pet, o meu usuarioId e os links HATEOAS

Cenário: [400] Pet sem nome é recusado
  Dado que estou autenticado
  Quando cadastro um pet com o nome vazio
  Então recebo 400 com erro de validação no campo Nome

Cenário: [200] Peso do pet é atualizado
  Dado que o pet Thor está cadastrado
  Quando atualizo a idade para 12 e o peso para 30
  Então recebo 200 com os dados atualizados

Cenário: [200] Lista de pets filtrada e paginada
  Dado que existem pets cadastrados
  Quando listo os pets da espécie Cachorro, ordenados por nome, 2 por página
  Então recebo 200 só com cachorros, no máximo 2 itens e os dados de paginação

Cenário: [204] Pet é removido
  Dado que o pet Thor está cadastrado
  Quando removo o pet
  Então recebo 204 e, ao consultar o pet de novo, recebo 404

Cenário: [401] Acesso sem token é negado
  Dado que não estou autenticado
  Quando listo os pets
  Então recebo 401

Cenário: [403] Tutor não altera pet de outro tutor
  Dado que o pet Thor pertence à Tutora A
  Quando a Tutora B tenta atualizar o Thor
  Então recebo 403 com a mensagem "Apenas o tutor do pet ou um veterinário pode realizar esta operação."
```

## US3: Agendamento de consultas

```gherkin
Cenário: [201] Consulta é agendada para pet existente
  Dado que o pet Thor está cadastrado
  Quando agendo uma consulta com a Dra. Carla Souza para 10/11/2026 às 10h
  Então recebo 201 com a consulta criada e o link para o pet

Cenário: [404] Consulta para pet inexistente é recusada
  Dado que não existe pet com id 999999
  Quando agendo uma consulta para o pet 999999
  Então recebo 404 com a mensagem "Pet com ID 999999 não foi encontrado(a)."
```

## US4: Avaliação de risco com IA (CLYVO AI)

```gherkin
Cenário: [201] Sintomas graves geram prioridade Alta
  Dado que o pet Fred (cachorro, 10 anos) está cadastrado
  Quando peço a avaliação com histórico DoencaCronica e sintomas "dificuldade para respirar", "fraqueza" e "febre"
  Então recebo 201 com prioridade Alta, o fator "Sinal grave: dificuldade para respirar" e a recomendação de procurar atendimento

Cenário: [400] Espécie não suportada pela IA é recusada
  Dado que o pet Kiwi (calopsita) está cadastrado
  Quando peço a avaliação de risco
  Então recebo 400 com a mensagem "A avaliação de risco da IA está disponível apenas para cães e gatos."
```

---

## Casos de teste (Azure Test Plans)

Passos comuns: acessar o Swagger e, nos casos autenticados, fazer login e clicar em **Authorize** com o token.

| # | Caso de teste | Entrada (parâmetros) | Saída esperada |
|---|---|---|---|
| 1 | [201] Cadastro de usuário válido | `@nome`, `@email`, `@senha` (Tutor A) | 201, perfil Tutor, sem senha na resposta |
| 2 | [409] E-mail já cadastrado | `@email` = ana.qa@petcare.com | 409 + mensagem de e-mail já cadastrado |
| 3 | [200] Login com credenciais válidas | `@email`, `@senha` | 200 + `token`, `tipo` = Bearer |
| 4 | [401] Login com senha incorreta | `@senha` = SenhaErrada1 | 401 + "E-mail ou senha inválidos." |
| 5 | [201] Pet válido é cadastrado | Pet A | 201, `id`, `usuarioId`, 6 links HATEOAS |
| 6 | [400] Pet sem nome é recusado | `@nome` = "" | 400, erro em Nome |
| 7 | [200] Peso do pet é atualizado | `@idade` = 12, `@peso` = 30 | 200, peso 30 |
| 8 | [200] Lista de pets filtrada e paginada | `@especie` = Cachorro, `@tamanho` = 2 | 200, só cachorros, ≤ 2 itens, `paginacao` |
| 9 | [204] Pet é removido | `@petId` | 204; GET depois → 404 |
| 10 | [401] Acesso sem token é negado | sem token | 401 |
| 11 | [403] Tutor não altera pet de outro tutor | Tutor B + `@petId` do Tutor A | 403 |
| 12 | [201] Consulta é agendada para pet existente | `@petId`, data, veterinário, motivo | 201, link `pet` |
| 13 | [404] Consulta para pet inexistente é recusada | `@petId` = 999999 | 404 |
| 14 | [201] Sintomas graves geram prioridade Alta | Fred + 3 sintomas | 201, prioridade Alta |
| 15 | [400] Espécie não suportada pela IA é recusada | Kiwi (calopsita) | 400 |
