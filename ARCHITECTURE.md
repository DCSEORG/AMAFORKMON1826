# Azure Services Architecture

## Basic Deployment (deploy.sh)

```
┌─────────────────────────────────────────────────────────────────┐
│                        Azure Resource Group                      │
│                                                                   │
│  ┌──────────────────────────┐                                   │
│  │  App Service (Linux)     │                                   │
│  │  - .NET 8.0 Runtime      │                                   │
│  │  - HTTPS Only            │                                   │
│  │  - Standard S1 SKU       │                                   │
│  │  - Port 443              │                                   │
│  └────────┬─────────────────┘                                   │
│           │                                                       │
│           │ Uses Managed Identity                                │
│           ↓                                                       │
│  ┌──────────────────────────┐                                   │
│  │ User-Assigned Managed    │                                   │
│  │ Identity                 │                                   │
│  │ - Client ID configured   │                                   │
│  │ - No secrets required    │                                   │
│  └────────┬─────────────────┘                                   │
│           │                                                       │
│           │ Authenticates to                                     │
│           ↓                                                       │
│  ┌──────────────────────────┐                                   │
│  │ Azure SQL Database       │                                   │
│  │ - Entra ID Auth Only     │                                   │
│  │ - Basic Tier             │                                   │
│  │ - TLS 1.2+               │                                   │
│  │ - Firewall: Azure Svcs   │                                   │
│  └──────────────────────────┘                                   │
│           │                                                       │
│           └─ Database: ExpenseManagement                         │
│              - Tables: Users, Roles, Expenses,                   │
│                        Categories, Status                        │
│              - Stored Procedures for all operations              │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Full Deployment with GenAI (deploy-with-chat.sh)

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Azure Resource Group                          │
│                                                                       │
│  ┌──────────────────────────┐                                       │
│  │  App Service (Linux)     │                                       │
│  │  - .NET 8.0 Runtime      │                                       │
│  │  - Chat UI Enabled       │                                       │
│  │  - Function Calling      │                                       │
│  └────────┬─────────────────┘                                       │
│           │                                                           │
│           │ Uses Managed Identity                                    │
│           ↓                                                           │
│  ┌──────────────────────────┐                                       │
│  │ User-Assigned Managed    │◄──────────┐                           │
│  │ Identity                 │           │                           │
│  └────────┬─────────────────┘           │                           │
│           │                              │                           │
│           │ Auth to SQL                  │ Auth to GenAI             │
│           ↓                              │                           │
│  ┌──────────────────────────┐           │                           │
│  │ Azure SQL Database       │           │                           │
│  │ - ExpenseManagement DB   │           │                           │
│  └──────────────────────────┘           │                           │
│                                          │                           │
│  ┌──────────────────────────┐           │                           │
│  │ Azure OpenAI             │◄──────────┘                           │
│  │ - GPT-4o Model           │                                       │
│  │ - Sweden Central         │                                       │
│  │ - S0 SKU                 │                                       │
│  └──────────────────────────┘                                       │
│                                                                       │
│  ┌──────────────────────────┐                                       │
│  │ Azure Cognitive Search   │◄──────────┐                           │
│  │ - RAG Pattern Support    │           │                           │
│  │ - Basic SKU              │           │ Managed Identity           │
│  └──────────────────────────┘           │                           │
│                                          │                           │
└──────────────────────────────────────────┘                           │
```

## Data Flow

### User Request Flow:
1. User accesses App Service via HTTPS
2. Razor Pages render UI
3. User submits form or uses Chat UI
4. Application calls stored procedures via Managed Identity
5. Results returned and displayed

### Chat AI Flow:
1. User types natural language query in Chat UI
2. Request sent to Azure OpenAI with function definitions
3. GPT-4o determines which functions to call
4. Application executes API calls via stored procedures
5. Results formatted by AI and returned to user

## Security Architecture

```
┌────────────────────┐
│  User Browser      │
│  (HTTPS only)      │
└─────────┬──────────┘
          │ TLS 1.2+
          ↓
┌────────────────────┐
│  App Service       │
│  - No secrets      │
│  - MI Client ID    │
└─────────┬──────────┘
          │ Azure AD Token
          ↓
┌────────────────────┐
│  Azure Resources   │
│  - SQL Database    │
│  - OpenAI          │
│  - Search          │
└────────────────────┘
```

## Network Configuration

- **App Service**: Outbound to Azure SQL (port 1433), OpenAI (HTTPS)
- **SQL Server**: Firewall rule for Azure services (0.0.0.0)
- **All Services**: UK South region (except OpenAI in Sweden Central)
- **DNS**: All resources use Azure-provided FQDN
- **Encryption**: TLS 1.2+ for all connections

## Cost Considerations

### Basic Deployment:
- App Service Standard S1: ~$70/month
- Azure SQL Basic: ~$5/month
- **Total**: ~$75/month

### With GenAI:
- App Service Standard S1: ~$70/month
- Azure SQL Basic: ~$5/month
- Azure OpenAI S0: ~$0 + usage
- Cognitive Search Basic: ~$75/month
- **Total**: ~$150/month + OpenAI usage

## Monitoring & Diagnostics

All services integrate with:
- Azure Monitor
- Application Insights (when configured)
- Diagnostic Settings
- Azure AD Sign-in Logs

## Deployment Sequence

1. Resource Group Creation
2. App Service + Managed Identity
3. Azure SQL Server + Database
4. Wait 30s for SQL readiness
5. Firewall Configuration
6. Schema Import (Python + Azure AD)
7. Database Role Assignment
8. Stored Procedures Deployment
9. [Optional] OpenAI + Search Deployment
10. [Optional] App Settings Update for GenAI
11. Application Deployment (app.zip)

## High Availability Considerations

Current configuration:
- Single instance App Service (S1)
- Basic SQL Database (no geo-replication)
- Suitable for development/demo

For production:
- Upgrade to Premium App Service with multiple instances
- Use SQL Standard/Premium with geo-replication
- Add Azure Front Door or Application Gateway
- Enable zone redundancy where available
