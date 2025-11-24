![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# Expense Management System - Azure Cloud Native Application

A modernized expense management system built with ASP.NET Core 8.0, Azure SQL Database, and Azure OpenAI, demonstrating cloud-native application patterns on Azure.

## Features

### Core Functionality
- **Add Expenses**: Submit new expenses with amount, category, date, and description
- **View Expenses**: List all expenses with filtering capabilities
- **Approve/Reject Expenses**: Manager workflow for expense approvals
- **RESTful APIs**: Full API coverage with Swagger documentation
- **Modern UI**: Clean, responsive interface using modern web design patterns

### Azure Integration
- **Managed Identity Authentication**: Secure, keyless authentication to Azure SQL
- **Azure SQL Database**: Cloud-native relational database with Entra ID authentication
- **Azure OpenAI**: Optional AI-powered chat interface for natural language expense queries
- **Azure Cognitive Search**: Enhanced search capabilities (when GenAI is deployed)
- **App Service**: Scalable, managed web hosting

### Security & Compliance
- **Azure AD-only Authentication**: SQL Server configured for maximum security
- **No Stored Credentials**: Uses managed identities throughout
- **HTTPS-only**: All traffic encrypted in transit
- **MCAPS Policy Compliant**: Meets enterprise governance requirements

## Architecture

```
┌─────────────────┐
│   App Service   │
│   (.NET 8.0)    │
└────────┬────────┘
         │
         ├──────────> Azure SQL Database (Entra ID Auth)
         │
         ├──────────> Azure OpenAI (Optional)
         │
         └──────────> Azure Cognitive Search (Optional)
```

## Deployment

### Prerequisites
1. Azure subscription
2. Azure CLI installed and authenticated (`az login`)
3. Appropriate permissions to create resources

### Quick Start - Basic Deployment (Without GenAI)

```bash
# Clone the repository
git clone <repository-url>
cd AMAFORKMON1826

# Deploy infrastructure and application
chmod +x deploy.sh
./deploy.sh
```

The script will:
1. Create resource group
2. Deploy App Service with Managed Identity
3. Deploy Azure SQL Database with Entra ID authentication
4. Configure network and security settings
5. Import database schema and stored procedures
6. Deploy the application

**Access your application**: `https://<app-service-name>.azurewebsites.net/Index`

### Advanced Deployment (With GenAI Chat)

For the full experience including AI-powered chat:

```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

Additional resources deployed:
- Azure OpenAI (GPT-4o model in Sweden Central)
- Azure Cognitive Search (Basic tier)
- Chat UI with function calling capabilities

**Access chat interface**: `https://<app-service-name>.azurewebsites.net/Chat`

## Local Development

### Setup
1. Install .NET 8.0 SDK
2. Authenticate with Azure: `az login`
3. Update `appsettings.Development.json` with your SQL Server details
4. Run the application:

```bash
cd src/ExpenseManagement
dotnet run
```

The connection string in `appsettings.Development.json` uses `Authentication=Active Directory Default`, which will use your local Azure CLI credentials.

### API Documentation
When running locally, access Swagger UI at: `https://localhost:5001/swagger`

## API Endpoints

- `GET /api/expenses` - Get all expenses
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/pending` - Get pending expenses
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit expense for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense
- `GET /api/categories` - Get all categories
- `GET /api/statuses` - Get all statuses
- `GET /api/users` - Get all users

## Database Schema

The application uses the following tables:
- **Roles**: Employee and Manager roles
- **Users**: User accounts with role assignments
- **ExpenseCategories**: Travel, Meals, Supplies, Accommodation, Other
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected
- **Expenses**: Main expense records with amounts stored in minor units (pence)

All database interactions use stored procedures for security and maintainability.

## Technology Stack

- **Backend**: ASP.NET Core 8.0 (LTS)
- **Frontend**: Razor Pages with modern CSS
- **Database**: Azure SQL Database
- **Authentication**: Azure Managed Identity, Azure AD
- **API Documentation**: Swagger/OpenAPI
- **AI**: Azure OpenAI (GPT-4o)
- **Search**: Azure Cognitive Search
- **Infrastructure**: Azure Bicep

## Project Structure

```
├── infrastructure/          # Bicep IaC files
│   ├── main.bicep          # Main orchestration
│   ├── app-service.bicep   # App Service & Managed Identity
│   ├── azure-sql.bicep     # SQL Database configuration
│   └── genai.bicep         # OpenAI & Search resources
├── src/ExpenseManagement/  # Application code
│   ├── Controllers/        # API controllers
│   ├── Models/            # Data models
│   ├── Pages/             # Razor Pages UI
│   └── Services/          # Business logic
├── Database-Schema/       # SQL schema
├── deploy.sh             # Deployment script (basic)
├── deploy-with-chat.sh   # Deployment script (with AI)
└── app.zip              # Deployment package
```

## Error Handling

The application implements comprehensive error handling with fallback to dummy data when database connections fail. Error messages include:
- Detailed error descriptions
- Source file and method information
- Specific guidance for managed identity issues
- Sample data for continued testing

## Security Considerations

- SQL Server enforces Azure AD-only authentication
- Managed Identity eliminates credential management
- All connections use TLS 1.2+
- No secrets or connection strings with passwords
- Compliance with MCAPS governance policies

## Troubleshooting

### Database Connection Issues
If you see "Unable to connect to database" errors:
1. Verify managed identity is assigned to App Service
2. Check managed identity has database permissions (db_datareader, db_datawriter, EXECUTE)
3. Ensure `AZURE_CLIENT_ID` environment variable is set correctly
4. Verify SQL Server firewall rules allow Azure services

### Local Development Issues
- Ensure you're logged in with `az login`
- Verify your Azure AD account has access to the SQL Server
- Check the connection string in `appsettings.Development.json`

## Contributing

This project demonstrates app modernization patterns for legacy applications. Feel free to adapt the patterns for your own use cases.

## License

See LICENSE file for details.

## Credits

This project showcases how GitHub Copilot coding agents can modernize legacy applications using screenshots and database schemas to generate cloud-native Azure replacements.