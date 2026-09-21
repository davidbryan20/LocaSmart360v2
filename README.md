# LocaSmart360 (LocaWeb360)

Aplicação web ASP.NET Core MVC para lojistas de e-commerce (Locaweb/Tray) ingerirem dados de vendas, calcularem o risco de fraude de cada venda e auditarem os resultados.

As vendas são depositadas em um "data lake" baseado em arquivos JSON, passam por um pipeline de ETL que aplica regras comportamentais de fraude e são armazenadas em PostgreSQL (Supabase). Em seguida, o lojista pode revisar as vendas, auditar transações de risco e gerenciar o catálogo de produtos.

## Funcionalidades

- **Autenticação**: cadastro e login com sessão, política de senha forte e hash de senha SHA-256. Os usuários possuem um cargo (`Cargo`): `Lojista` (padrão) ou `Administrador`.
- **ETL de vendas / ingestão no data lake** (`/Etl/Integracao`): envio de arquivo JSON ou colagem do conteúdo. Os arquivos são salvos em `wwwroot/DataLake/Raw`, processados e pontuados, e depois movidos para `wwwroot/DataLake/Processed`. Somente administradores podem ingerir dados.
- **Motor de análise de fraude**: cada venda recebe uma pontuação de risco (0–100) e uma justificativa em texto. Veja [Regras de fraude](#regras-de-fraude).
- **Auditoria de vendas** (`/Vendas`): revisão de vendas, pontuações e justificativas, incluindo geolocalização e IP do cliente.
- **Catálogo de produtos** (`/Produtos`): criação, edição e exclusão de produtos (SKU, nome, categoria, palavras-chave de SEO).
- **Log de execuções do ETL**: cada execução do ETL é registrada em `EtlLogs` com status e quantidade de registros.
- **Modo demonstração offline**: se o banco de dados estiver inacessível, a tela de login exibe um selo de offline e uma conta de demonstração ainda consegue entrar.

## Tecnologias

- .NET 10 / ASP.NET Core MVC (views Razor, compilação em tempo de execução)
- Entity Framework Core 10 com Npgsql
- PostgreSQL, hospedado no Supabase
- Bootstrap (incluído em `wwwroot/lib`)

## Estrutura do projeto

```
LocaWeb360.slnx
LocaWeb360/
├── Controllers/     Autenticacao, Etl, Home, Produtos, Vendas
├── Data/            LocaSmartDbContext (EF Core)
├── DTOs/            Objetos de requisição/resposta do ETL
├── Migrations/      Migrações do EF Core
├── Models/          Usuario, Produto, Venda, EtlLog
├── Repositories/    Repositório de vendas
├── Services/        EtlService, AnaliseFraudeService
├── Utils/           Utilitário de hash de senha
├── Views/           Views Razor
└── wwwroot/DataLake/  Arquivos JSON Raw (recebidos) e Processed (processados)
```

## Primeiros passos

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Um banco de dados PostgreSQL (um projeto Supabase funciona)
- `dotnet-ef` para as migrações: `dotnet tool install --global dotnet-ef`

### Configuração

A aplicação lê a string de conexão do banco a partir da chave `SupabaseConnection` e não inclui nenhuma no repositório. Mantenha os segredos fora do controle de versão usando [user secrets do .NET](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd LocaWeb360
dotnet user-secrets set "ConnectionStrings:SupabaseConnection" "Host=<host>;Port=5432;Database=postgres;Username=<usuario>;Password=<senha>;SSL Mode=Require"

# Opcional: credenciais do login de demonstração offline
dotnet user-secrets set "ModoDemo:Email" "demo@example.com"
dotnet user-secrets set "ModoDemo:Senha" "<senha-demo>"
```

### Executando

```bash
cd LocaWeb360
dotnet ef database update   # aplica as migrações
dotnet run
```

A aplicação inicia em `http://localhost:5133` (ou `https://localhost:7082` com o perfil `https`) e abre na tela de login.

## Ingestão de dados de vendas

Envie um array JSON de vendas pela tela **Integração**. Exemplo (veja `wwwroot/DataLake/Processed/carga.json` para mais):

```json
[
  {
    "ClienteId": "CLI-06",
    "ProdutoId": "b44fcf01-e8e9-4dd5-b6e5-1b694dad912b",
    "ValorTotal": 120.00,
    "DataVenda": "2026-06-04T12:00:00Z",
    "Lat": -23.5505,
    "Lon": -46.6333,
    "IpCliente": "177.185.10.15"
  }
]
```

O `ProdutoId` deve referenciar um produto existente. As vendas são ordenadas por data antes do processamento, para que o histórico de cada cliente seja avaliado em ordem cronológica, e vendas cujo `VendaId` já existe são ignoradas.

## Regras de fraude

Cada venda recebe uma pontuação de 0 a 100 e é classificada como **NORMAL** (abaixo de 40), **ALERTA / suspeita** (40–74) ou **CRÍTICO / bloqueada** (75+). As regras são cumulativas e a pontuação é limitada a 100.

| Regra | Gatilho |
| --- | --- |
| Deslocamento impossível | Velocidade entre esta venda e a última do cliente acima de 900 km/h (e mais de 50 km), ou vendas simultâneas a mais de 20 km de distância |
| Velocidade por IP | Outra venda do mesmo IP em menos de 10 minutos |
| Teste de cartão | Microtransação de R$ 15,00 ou menos |
| Golpe pós-microtransação | Compra alta logo após uma microtransação (acima de 2x ou 3x o ticket médio do cliente) |
| Cooldown | Valor 2x/3x ou mais acima da compra anterior em até 24h, ou 3x/4x ou mais após 24h |
| Cold start | Primeira compra de um cliente novo acima de R$ 1.000,00 |
| Horário de risco | Compra entre 00:00 e 05:59 que não condiz com o padrão habitual do cliente |

As regras estão em `Services/EtlService.cs`.
