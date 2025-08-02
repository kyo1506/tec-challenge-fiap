# 🎮 Tech Challenge - Fase 2

**Plataforma de Venda de Jogos Digitais e Gestão de Transações Financeiras**

---

## 📌 Visão Geral

A API desenvolvida oferece uma solução completa para comercialização de jogos digitais, com funcionalidades que abrangem:

- 🔐 **Autenticação e Autorização**: Registro, login, redefinição de senha e confirmação de e-mail 
- 🕹️ **Gestão de Jogos**: Cadastro completo do catálogo de jogos 
- 💸 **Promoções**: Criação e gerenciamento de descontos promocionais 
- 💳 **Transações Financeiras**: Compra, reembolso, depósito e saque de saldo 
- 📚 **Biblioteca do Usuário**: Armazenamento e gerenciamento dos jogos adquiridos

---

## 🧱 Estrutura do Projeto

O projeto segue os princípios de **Domain-Driven Design (DDD)** e utiliza **injeção de dependência** para garantir modularidade, coesão e manutenção facilitada.

### 🔧 Camadas

- **Application** – Camada de orquestração da lógica de aplicação 
- **Domain** – Regras de negócio e entidades do domínio 
- **Data** – Implementações de repositórios e acesso a dados 
- **Infrastructure** – Integrações externas (como serviços de e-mail) 
- **Shared** – DTOs, modelos base, Requests/Responses e validações
- **Tests** – Contém os testes unitários da aplicação

---

## 🔗 Endpoints da API

### 🛡️ Autenticação (`/v1/auth`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar todos os usuários |
| GET    | `/{id}` | Obter usuário específico com permissões |
| PUT    | `/{id}` | Atualizar usuário |
| DELETE | `/{id}` | Excluir usuário |
| POST   | `/register` | Registrar nova conta |
| POST   | `/login` | Login do usuário |
| POST   | `/refresh-token` | Renovar token JWT |
| POST   | `/first-access` | Redefinir senha no primeiro acesso |
| GET    | `/reset-password/{email}` | Enviar link de redefinição de senha |
| POST   | `/reset-password` | Redefinir senha |
| GET    | `/confirm-email/{email}` | Enviar link de confirmação de e-mail |
| POST   | `/confirm-email` | Confirmar e-mail |

### 🎮 Jogos (`/v1/games`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar todos os jogos |
| POST   | `/` | Criar novo jogo |
| GET    | `/{id}` | Obter jogo por ID |
| PUT    | `/{id}` | Atualizar jogo |
| DELETE | `/{id}` | Excluir jogo |

### 🏷️ Promoções (`/v1/promotions`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/` | Listar promoções ativas |
| POST   | `/` | Criar nova promoção |
| GET    | `/{id}` | Obter promoção por ID |
| PUT    | `/{id}` | Atualizar promoção |
| DELETE | `/{id}` | Excluir promoção |
| POST   | `/{promotionId}/promotion-games` | Adicionar jogos à promoção |
| PUT    | `/promotion-games/{promotionGameId}` | Atualizar item da promoção |
| DELETE | `/promotion-games/{promotionGameId}` | Remover jogo da promoção |

### 💰 Transações (`/v1/transactions`)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST   | `/purchase` | Comprar jogo |
| PUT    | `/refund-purchase` | Solicitar reembolso |
| POST   | `/deposit` | Depositar saldo |
| PUT    | `/withdraw` | Sacar saldo |

### 📚 Biblioteca (`/v1/user-libraries`)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET    | `/{userId}` | Consultar jogos adquiridos |

---

## 📦 Modelos de Dados

### 🔐 Autenticação
- `LoginDto`: E-mail e senha 
- `CreateUserDto`: Cadastro de usuário com e-mail, permissões e role 
- `UserDto`: Dados completos do usuário 
- `ChangePasswordDto`: Redefinição de senha 

### 🎮 Jogos
- `GameAddRequest`: Nome e preço do jogo 
- `GameUpdateRequest`: Dados completos do jogo 
- `GameResponse`: ID, nome, status, preço e datas 

### 🏷️ Promoções
- `PromotionAddRequest`: Nome, datas e jogos da promoção 
- `PromotionGameAddRequest`: ID do jogo e percentual de desconto 
- `PromotionResponse`: Dados da promoção 

### 💳 Transações
- `PurchaseGameRequest`: ID do usuário, jogo e promoção (opcional) 
- `BalanceRequest`: ID do usuário e valor 
- `RefundPurchaseRequest`: ID do usuário e jogo 

---

## ⚙️ Recursos Técnicos

- **Linguagem**: C# 
- **Framework**: ASP.NET Core (.NET 9) 
- **Arquitetura**: MVC + DDD 
- **Testes**: TDD 
- **Autenticação**: JWT com refresh token 
- **Validação**: Data Annotations, FluentValidation, EF Mapping 
- **Documentação**: OpenAPI / Swagger 3.0.4 
- **Serviços**: Serviço de e-mail mockado (por segurança)
- **Banco de dados**: SQL Server

---

## ✅ Testes

Para garantir a qualidade e a confiabilidade do sistema, a aplicação foi desenvolvida seguindo os princípios de **Test-Driven Development (TDD)**, com uma cobertura abrangente de testes unitários.

Os testes foram implementados utilizando as seguintes ferramentas e bibliotecas:

* **xUnit**: Framework de testes para .NET.
* **Moq**: Biblioteca para criação de mocks, facilitando o isolamento de dependências e o teste de unidades de código.
* **FluentAssertions**: Biblioteca que oferece uma sintaxe fluente e legível para a verificação de resultados de testes.

Cada serviço e funcionalidade crítica possui seus respectivos testes, garantindo que as regras de negócio sejam validadas e que o comportamento do sistema seja o esperado em diferentes cenários, incluindo casos de sucesso, falhas, exceções e validações de domínio.

---

## 📈 Monitoramento e Health Checks

A aplicação incorpora **Health Checks** para monitorar a saúde dos seus componentes e dependências críticas. Isso permite uma visibilidade em tempo real sobre o status da API e de serviços externos, como o banco de dados.

Utilizamos as seguintes ferramentas para monitoramento:

* **ASP.NET Core Health Checks**: Para verificar a disponibilidade de serviços e dependências.

* **HealthChecksUI**: Uma interface de usuário para visualizar o status dos Health Checks de forma intuitiva.

### Endpoints de Monitoramento

* **`/health`**: Retorna um JSON detalhado com o status de cada Health Check configurado.

---

## 🧠 Regras de Negócio

### 🔐 Autenticação
- Senha segura (mín. 8 caracteres, maiúscula, minúscula, número e caractere especial) 
- Confirmação de e-mail obrigatória 
- Controle de acesso por roles e claims 

### 💳 Transações
- Validação de saldo 
- Prevenção de compras duplicadas 
- Reembolso com regras de elegibilidade 
- Aplicação automática de promoções válidas 

### 🏷️ Promoções
- Datas válidas (início < fim) 
- Descontos entre 1% e 100% 
- Proibição de remover jogos com compras vinculadas 

### 🎮 Jogos
- Nome único por jogo 

### 📚 Biblioteca
- Sem duplicação de jogos para o mesmo usuário 

---

## 🚀 Como Executar

1. Clone o repositório 
2. Configure a string de conexão em `appsettings.json` 
3. Execute as migrações do banco de dados 
4. Compile e execute o projeto 
5. Acesse a documentação Swagger: `/swagger`

**Após a execução das migrações, por fim, ao executar o projeto Application pela primeira vez, o serviço de Seed gerará os usuários abaixo:**

ADMIN
```json
{
  "email": "vinicius_pinheiro05@hotmail.com",
  "password": "Default@123"
}
```

USER
```json
{
  "email": "vinicius_pinheiro02@hotmail.com",
  "password": "Default@123"
}
```

Utilize-os para fazer login e testar as funcionalidades da aplicação.

---

## 🔐 Autenticação da API

1. Acesse `/v1/auth/login` e faça login 
2. Copie o `accessToken` retornado 
3. Utilize no header `Authorization: Bearer {seu_token}` 
4. Quando necessário, renove com `/refresh-token`

---

## 👥 Contato

- **Vinicius Freire** - **Willian Costa**

📄 Licença: MIT 
🧪 Desenvolvido com foco em escalabilidade, segurança e boas práticas RESTful.
