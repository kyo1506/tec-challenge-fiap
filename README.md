# Tech Challenge - Fase 1

Este projeto é a primeira fase da criação da plataforma de venda de jogos digitais e gestão de transações financeiras.

## Visão Geral

A API desenvolvida oferece uma plataforma completa para venda de jogos digitais, incluindo:

- **Autenticação e Autorização**: Sistema completo de registro, login e gerenciamento de usuários
- **Gestão de Jogos**: CRUD completo para catálogo de jogos
- **Sistema de Promoções**: Criação e gerenciamento de promoções com descontos
- **Transações Financeiras**: Compra, reembolso, depósito e saque
- **Biblioteca do Usuário**: Gerenciamento da coleção de jogos adquiridos

## Estrutura do Projeto

O projeto segue a abordagem **Domain-Driven Design (DDD)** com **injeção de dependência** para promover modularidade, coesão e fácil manutenção. As principais camadas são:

- **Application** - Responsável pela apresentação e lógicas de aplicação
- **Data** - Gerencia a persistência e conexão com o banco de dados
- **Domain** - Contém a lógica de domínio e as entidades
- **Infrastructure** - Gerencia a conexão e lógica de serviços externos
- **Shared** - DTOs, classes genéricas e implementação de Requests e Responses

## Endpoints da API

### Autenticação (`/api/v1/auth`)
- **GET** `/` - Listar todos os usuários
- **GET** `/{id}` - Obter usuário específico e suas permissões
- **PUT** `/{id}` - Atualizar dados do usuário
- **DELETE** `/{id}` - Excluir usuário
- **POST** `/register` - Registrar nova conta
- **POST** `/login` - Fazer login na aplicação
- **POST** `/refresh-token` - Renovar token de acesso
- **POST** `/first-access` - Redefinir senha no primeiro acesso
- **GET** `/reset-password/{email}` - Enviar link de redefinição de senha
- **POST** `/reset-password` - Redefinir senha do usuário
- **GET** `/confirm-email/{email}` - Enviar link de confirmação de email
- **POST** `/confirm-email` - Confirmar email do usuário

### Jogos (`/api/v1/games`)
- **GET** `/` - Listar todos os jogos disponíveis
- **POST** `/` - Criar novo jogo
- **GET** `/{id}` - Obter jogo específico
- **PUT** `/{id}` - Atualizar jogo existente
- **DELETE** `/{id}` - Excluir jogo

### Promoções (`/api/v1/promotions`)
- **GET** `/` - Listar todas as promoções ativas
- **POST** `/` - Criar nova promoção
- **GET** `/{id}` - Obter promoção específica
- **PUT** `/{id}` - Atualizar promoção existente
- **DELETE** `/{id}` - Excluir promoção
- **POST** `/{promotionId}/promotion-games` - Adicionar jogos à promoção
- **PUT** `/promotion-games/{promotionGameId}` - Atualizar item da promoção
- **DELETE** `/promotion-games/{promotionGameId}` - Remover jogo da promoção

### Transações (`/api/v1/transactions`)
- **POST** `/purchase` - Processar compra de jogo
- **PUT** `/refund-purchase` - Processar reembolso de compra
- **POST** `/deposit` - Depositar fundos na conta do usuário
- **PUT** `/withdraw` - Sacar fundos da conta do usuário

### Biblioteca do Usuário (`/api/v1/user-libraries`)
- **GET** `/{userId}` - Obter biblioteca de jogos do usuário

## Modelos de Dados Principais

### Autenticação
- **LoginDto**: Email e senha para login
- **CreateUserDto**: Dados para criação de usuário (email, role, claims)
- **UserDto**: Informações completas do usuário
- **ChangePasswordDto**: Dados para redefinição de senha

### Jogos
- **GameAddRequest**: Nome e preço para criação
- **GameUpdateRequest**: Dados completos para atualização
- **GameResponse**: Informações do jogo (id, nome, preço, status, datas)

### Promoções
- **PromotionAddRequest**: Nome, datas e jogos em promoção
- **PromotionGameAddRequest**: ID do jogo e percentual de desconto
- **PromotionResponse**: Dados completos da promoção

### Transações
- **PurchaseGameRequest**: IDs do usuário, jogo e promoção (opcional)
- **BalanceRequest**: ID do usuário e valor para depósito/saque
- **RefundPurchaseRequest**: IDs do usuário e jogo para reembolso

## Recursos Técnicos

- **Linguagem**: C#
- **Framework**: ASP.NET Core
- **Arquitetura**: Domain-Driven Design (DDD) com injeção de dependência
- **Autenticação**: JWT (JSON Web Tokens) com Refresh Token
- **Documentação**: OpenAPI/Swagger 3.0.4
- **Validação**: Data Annotations e validações de negócio

## Regras de Negócio

### Autenticação
- Senhas devem conter pelo menos 8 caracteres, incluindo maiúscula, minúscula, número e caractere especial
- Sistema de confirmação de email obrigatório
- Controle de acesso baseado em roles e claims

### Transações
- Verificação de saldo suficiente para compras e saques
- Prevenção de compra duplicada do mesmo jogo
- Sistema de reembolso com validações de elegibilidade
- Aplicação automática de descontos quando há promoção ativa

### Promoções
- Validação de datas (início deve ser anterior ao fim)
- Percentual de desconto entre 1% e 100%
- Controle de conflitos ao remover jogos com transações existentes

## Executando o Projeto

1. Clone este repositório
2. Configure a string de conexão no arquivo `appsettings.json`
3. Execute as migrações do banco de dados
4. Compile e execute o projeto
5. Acesse a documentação Swagger em `/swagger`

## Autenticação da API

A API utiliza autenticação Bearer Token. Para acessar endpoints protegidos:

1. Faça login através do endpoint `/api/v1/auth/login`
2. Use o `accessToken` retornado no header Authorization: `Bearer {seu_token}`
3. Renove o token quando necessário usando o `refreshToken`

## Contato

**Desenvolvedor**: Vinicius Freire

**Desenvolvedor**: Willian Costa

**Licença**: MIT

Este projeto foi desenvolvido com foco em escalabilidade, segurança e facilidade de manutenção, seguindo as melhores práticas de desenvolvimento de APIs REST.
