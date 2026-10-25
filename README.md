# Ludoteca API

Checkpoint 5 da disciplina Advanced Business Development with .NET (FIAP, 2026).

## Integrantes

| Nome | RM |
|---|---|
| Hebert Lopes do Santos | 563192 |
| Marcus Vinícius Vila Nova da Silva | 558771 |
| Nicolas Monteiro Ramiro | 562380 |
| Gabriel Robertoni Padilha | 566293 |
| Bruno Ferreira | 563489 |

## Descrição do projeto

A Ludoteca API é uma API REST em ASP.NET Core (.NET 8, com Controllers) para gerenciar uma ludoteca, isto é, uma biblioteca de jogos de tabuleiro em que os membros pegam jogos emprestados.

Entidades do domínio:

- **Jogo**: título, editora, categoria, número de jogadores, duração média e disponibilidade.
- **Membro**: dados de contato e endereço, preenchido automaticamente a partir do CEP pelo serviço externo ViaCEP.
- **Empréstimo**: liga um jogo a um membro, com data de empréstimo, prazo de devolução e data de devolução.
- **Usuário**: quem acessa a API, com perfil `Administrador` ou `Atendente`.

Regras de negócio:

- Um jogo só pode ser emprestado se estiver disponível; o empréstimo o torna indisponível e a devolução o libera.
- Cada membro pode ter no máximo 3 empréstimos ativos ao mesmo tempo.
- O prazo do empréstimo vai de 1 a 30 dias (padrão de 7). Passado o prazo sem devolução, o empréstimo aparece com status `Atrasado`.
- Título de jogo, e-mail de membro e e-mail de usuário são únicos.
- Jogos e membros com empréstimos registrados não podem ser excluídos.
- Cadastrar, alterar e excluir jogos, e excluir membros, exige o perfil `Administrador`. As demais operações exigem apenas estar autenticado.

## Arquitetura e componentes

A solução segue Clean Architecture, com quatro camadas e dependências apontando sempre para dentro:

```
Ludoteca.sln
src/
  Ludoteca.Domain           entidades, regras de negócio e exceções de domínio (sem dependências)
  Ludoteca.Application      serviços, DTOs, Operation Result, PageResult e contratos (interfaces)
  Ludoteca.Infrastructure   Entity Framework Core com Oracle, repositórios, ViaCEP, JWT e hash de senha
  Ludoteca.Api              controllers, HATEOAS, autenticação, tratamento global de erros e Swagger
tests/
  Ludoteca.Tests.Unit         testes de unidade (xUnit e Moq)
  Ludoteca.Tests.Integration  testes funcionais da API (xUnit e WebApplicationFactory) no banco real
```

| Camada | Referencia | Responsabilidade |
|---|---|---|
| Domain | nada | Entidades `Jogo`, `Membro`, `Emprestimo` e `Usuario` com suas regras; exceções customizadas |
| Application | Domain | Casos de uso (serviços), DTOs, `OperationResult`, `PageResult`, interfaces de repositórios e de serviços externos |
| Infrastructure | Application | Persistência, repositórios, cliente HTTP do ViaCEP, geração de JWT, hash de senha |
| Api | Application e Infrastructure | Controllers, HATEOAS, configuração de autenticação e composição das dependências |

### Operation Result

Os serviços da camada Application nunca devolvem exceções aos controllers: retornam `OperationResult` ou `OperationResult<T>`, com `IsSuccess`, `Data` e um `Error` (código, mensagem e tipo). O controller converte o resultado em HTTP por meio de `ProblemDetails`:

| Tipo do erro | Status HTTP |
|---|---|
| Validação | 400 |
| Não autorizado | 401 |
| Não encontrado | 404 |
| Conflito | 409 |
| Regra de negócio | 422 |
| Serviço externo indisponível | 503 |

### Erros customizados

As exceções de domínio herdam de `ErroDeDominioException` (código, mensagem e tipo): `RegraDeNegocioException`, `EntidadeNaoEncontradaException` e `ConflitoDeDadosException`. Os repositórios usam as duas abordagens:

- **Erro por retorno**: as consultas (`ObterPorIdAsync`, `ObterPorEmailAsync` e similares) devolvem `OperationResult` com o erro de "não encontrado" dentro do retorno.
- **Erro por lançamento**: os comandos (`AdicionarAsync`, `AtualizarAsync`, `RemoverAsync`) lançam exceções específicas, por exemplo `ConflitoDeDadosException` para título ou e-mail duplicado e para exclusão de registro com empréstimos, e `EntidadeNaoEncontradaException` ao remover um id inexistente. Os serviços capturam essas exceções e as convertem em `OperationResult`. Se alguma escapar, o `GlobalExceptionHandler` da API a transforma em `ProblemDetails`, e qualquer outra exceção vira um 500 genérico.

### Chamada de serviço externo

O `ViaCepService` (camada Infrastructure) consulta `https://viacep.com.br/ws/{cep}/json/` por um `HttpClient` tipado, com o pipeline de resiliência `AddStandardResilienceHandler`: retry com backoff (3 tentativas), timeout por tentativa, timeout total e circuit breaker. Respostas são tratadas assim: CEP inexistente vira 404, CEP inválido vira 400, e indisponibilidade, timeout ou circuito aberto viram 503. CEPs consultados com sucesso ficam em cache em memória por 24 horas.

O serviço é usado no cadastro e na atualização de membros (o endereço é preenchido a partir do CEP) e diretamente em `GET /api/enderecos/{cep}`.

### PageResult

As listagens retornam `PageResult<T>`, com `Data`, `Page`, `PageSize`, `ItemCount`, `TotalRecords`, `TotalPages`, `HasPrevious` e `HasNext`. A paginação acontece no banco (`Skip`/`Take`) e `pageSize` é limitado a 50.

### HATEOAS

Toda resposta traz links de navegação. Um recurso vem como `{ "data": ..., "links": [...] }` e uma coleção como `{ "data": [recursos], "pagination": {...}, "links": [...] }`, com os links `self`, `first`, `prev`, `next` e `last`. Os links dependem do estado do recurso e do perfil do usuário: um jogo disponível oferece `emprestar`, um empréstimo ativo oferece `devolver`, e `atualizar` e `remover` só aparecem para o `Administrador`.

```json
{
  "data": { "id": 1, "titulo": "Catan", "disponivel": true },
  "links": [
    { "rel": "self", "href": "http://localhost:5188/api/jogos/1", "method": "GET" },
    { "rel": "emprestar", "href": "http://localhost:5188/api/emprestimos", "method": "POST" },
    { "rel": "remover", "href": "http://localhost:5188/api/jogos/1", "method": "DELETE" }
  ]
}
```

### Otimização

- Consultas de leitura com `AsNoTracking`.
- Paginação e contagem executadas no banco, nunca em memória.
- Listagem de empréstimos com projeção (`Select`) apenas das colunas usadas, sem carregar `Jogo` e `Membro` inteiros.
- Índices nas colunas de busca e de junção: título do jogo (único), categoria, e-mail de membros e de usuários (únicos), nome do membro e, em empréstimos, `(JOGO_ID, DATA_DEVOLUCAO)` e `(MEMBRO_ID, DATA_DEVOLUCAO)`.
- Token de concorrência na disponibilidade do jogo, que impede dois empréstimos simultâneos do mesmo jogo.
- Cache em memória das consultas de CEP e compressão das respostas HTTP.

### Autenticação e autorização

Autenticação JWT (Bearer). As senhas são guardadas com hash (`PasswordHasher`). Dois perfis:

| Perfil | Pode |
|---|---|
| Atendente | Consultar tudo, cadastrar e alterar membros, registrar empréstimos e devoluções |
| Administrador | Tudo o que o Atendente pode, mais cadastrar, alterar e excluir jogos e excluir membros |

Sem token a API responde 401; com token de perfil insuficiente, 403.

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| POST | `/api/auth/login` | Público | Autentica e devolve o token JWT |
| POST | `/api/auth/registrar` | Público | Cadastra um usuário com perfil Atendente |
| GET | `/api/jogos` | Autenticado | Lista jogos (`titulo`, `categoria`, `disponivel`, `page`, `pageSize`) |
| GET | `/api/jogos/{id}` | Autenticado | Consulta um jogo |
| POST | `/api/jogos` | Administrador | Cadastra um jogo |
| PUT | `/api/jogos/{id}` | Administrador | Atualiza um jogo |
| DELETE | `/api/jogos/{id}` | Administrador | Exclui um jogo sem empréstimos |
| GET | `/api/membros` | Autenticado | Lista membros (`nome`, `page`, `pageSize`) |
| GET | `/api/membros/{id}` | Autenticado | Consulta um membro |
| POST | `/api/membros` | Autenticado | Cadastra um membro (endereço pelo CEP) |
| PUT | `/api/membros/{id}` | Autenticado | Atualiza um membro |
| DELETE | `/api/membros/{id}` | Administrador | Exclui um membro sem empréstimos |
| GET | `/api/emprestimos` | Autenticado | Lista empréstimos (`membroId`, `jogoId`, `ativos`, `page`, `pageSize`) |
| GET | `/api/emprestimos/{id}` | Autenticado | Consulta um empréstimo |
| POST | `/api/emprestimos` | Autenticado | Registra um empréstimo |
| POST | `/api/emprestimos/{id}/devolucao` | Autenticado | Registra a devolução |
| GET | `/api/enderecos/{cep}` | Autenticado | Consulta um endereço no ViaCEP |

## Como rodar o projeto

Pré-requisitos:

- SDK do .NET 8.
- Acesso de rede ao banco Oracle da FIAP (`oracle.fiap.com.br:1521`) e à internet, para o ViaCEP.

```bash
dotnet restore
dotnet build
dotnet run --project src/Ludoteca.Api
```

A API sobe em `http://localhost:5188` e o Swagger abre na raiz (`/`). Ao iniciar, a aplicação aplica as migrations no banco (criando as tabelas `LUDO_USUARIO`, `LUDO_JOGO`, `LUDO_MEMBRO` e `LUDO_EMPRESTIMO`) e cria o usuário administrador inicial.

Para usar o Swagger: faça login em `POST /api/auth/login` com o administrador abaixo, copie o `token` da resposta, clique em **Authorize** e informe apenas o token.

| Administrador inicial | |
|---|---|
| E-mail | `admin@ludoteca.com.br` |
| Senha | `Admin@12345` |

## Como executar os testes

```bash
dotnet test
```

Para rodar cada projeto separadamente:

```bash
dotnet test tests/Ludoteca.Tests.Unit
dotnet test tests/Ludoteca.Tests.Integration
```

- Os **testes de unidade** cobrem as regras do domínio, o `OperationResult`, o `PageResult`, os serviços da Application (com repositórios simulados com Moq), o cliente do ViaCEP (com o pipeline de resiliência real) e a geração de JWT.
- Os **testes de integração** sobem a API completa com `WebApplicationFactory` e executam as requisições HTTP no próprio banco Oracle da aplicação, sem SQLite nem banco em memória. Cobrem autenticação, autorização por perfil, CRUD, paginação, HATEOAS, regras de empréstimo e tratamento de erros. Só a chamada de rede ao ViaCEP é substituída por um dublê, para o resultado não depender da internet.
- Os dados criados pelos testes usam os prefixos `teste.cp5.` (e-mails) e `Teste CP5 ` (títulos) e são removidos ao final; o administrador do seed é preservado.

## Configurações necessárias

Tudo fica em `src/Ludoteca.Api/appsettings.json`. Qualquer valor pode ser sobrescrito por variável de ambiente, usando `__` como separador (por exemplo, `ConnectionStrings__OracleConnection`).

| Chave | Descrição |
|---|---|
| `ConnectionStrings:OracleConnection` | String de conexão do banco Oracle (banco de dados da aplicação e dos testes) |
| `Banco:MigrarNaInicializacao` | Se `true`, aplica as migrations ao iniciar a aplicação |
| `Jwt:Chave`, `Jwt:Emissor`, `Jwt:Audiencia`, `Jwt:ExpiraEmMinutos` | Parâmetros do token JWT; use uma chave longa (recomendado ao menos 32 caracteres) |
| `ViaCep:UrlBase` | Endereço do serviço externo (não exige chave de acesso) |
| `ViaCep:TentativasDeRetry`, `ViaCep:AtrasoEntreTentativasMilissegundos` | Política de retry |
| `ViaCep:TimeoutPorTentativaSegundos`, `ViaCep:TimeoutTotalSegundos` | Timeouts do serviço externo |
| `ViaCep:CacheEmHoras` | Tempo de cache das consultas de CEP |
| `Administrador:Nome`, `Administrador:Email`, `Administrador:Senha` | Usuário administrador criado na inicialização |

Banco de dados: o provider Oracle é configurado com compatibilidade 19c. As migrations ficam em `src/Ludoteca.Infrastructure/Persistence/Migrations` e usam a tabela de controle `LUDO_MIGRATIONS_HISTORY`. Para aplicá-las manualmente, sem subir a API:

```bash
dotnet ef database update --project src/Ludoteca.Infrastructure --startup-project src/Ludoteca.Api
```

## Vídeo de demonstração

Link do vídeo (máximo de 5 minutos): https://youtu.be/MpkBaCUwtCc
