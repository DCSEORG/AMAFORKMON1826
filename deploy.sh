#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Expense Management System Deployment${NC}"
echo -e "${GREEN}========================================${NC}"

# Check if user is logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${RED}Error: Not logged in to Azure. Please run 'az login' first.${NC}"
    exit 1
fi

# Get current user info for SQL admin
ADMIN_USER=$(az account show --query user.name -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo -e "${YELLOW}Deploying as: $ADMIN_USER${NC}"
echo -e "${YELLOW}Object ID: $ADMIN_OBJECT_ID${NC}"

# Configuration
RESOURCE_GROUP="rg-expense-management"
LOCATION="uksouth"

# Create resource group if it doesn't exist
echo -e "\n${GREEN}Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy infrastructure
echo -e "\n${GREEN}Deploying infrastructure (App Service + SQL)...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infrastructure/main.bicep \
    --parameters location=$LOCATION \
    --parameters deployGenAI=false \
    --parameters adminLogin="$ADMIN_USER" \
    --parameters adminObjectId="$ADMIN_OBJECT_ID" \
    --query properties.outputs \
    -o json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME="mid-AppModAssist-$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value' | cut -c1-8)"

echo -e "\n${GREEN}Deployment completed!${NC}"
echo -e "${YELLOW}App Service: $APP_SERVICE_NAME${NC}"
echo -e "${YELLOW}SQL Server: $SQL_SERVER_FQDN${NC}"
echo -e "${YELLOW}Database: $DATABASE_NAME${NC}"
echo -e "${YELLOW}Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID${NC}"

# Configure App Service settings
echo -e "\n${GREEN}Configuring App Service settings...${NC}"
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN};Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};"

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

echo -e "${GREEN}App settings configured.${NC}"

# Wait for SQL Server to be fully ready
echo -e "\n${YELLOW}Waiting 30 seconds for SQL Server to be fully ready...${NC}"
sleep 30

# Add current machine's IP to SQL firewall
echo -e "\n${GREEN}Adding current machine IP to SQL firewall...${NC}"
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "LocalMachine" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none
echo -e "${GREEN}Firewall rule added for IP: $MY_IP${NC}"

# Install required Python packages
echo -e "\n${GREEN}Installing Python dependencies...${NC}"
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server and database names
echo -e "\n${GREEN}Updating Python scripts with deployment values...${NC}"
sed -i.bak "s/sql-expense-example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/ExpenseManagement/${DATABASE_NAME}/g" run-sql.py && rm -f run-sql.py.bak

sed -i.bak "s/sql-expense-example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/ExpenseManagement/${DATABASE_NAME}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak

sed -i.bak "s/sql-expense-example.database.windows.net/${SQL_SERVER_FQDN}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/ExpenseManagement/${DATABASE_NAME}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

sed -i.bak "s/MANAGED-IDENTITY-NAME/${MANAGED_IDENTITY_NAME}/g" script.sql && rm -f script.sql.bak

# Import database schema
echo -e "\n${GREEN}Importing database schema...${NC}"
python3 run-sql.py

# Configure database roles for managed identity
echo -e "\n${GREEN}Configuring database roles for managed identity...${NC}"
python3 run-sql-dbrole.py

# Deploy stored procedures
echo -e "\n${GREEN}Deploying stored procedures...${NC}"
python3 run-sql-stored-procs.py

# Deploy application code
if [ -f "app.zip" ]; then
    echo -e "\n${GREEN}Deploying application code...${NC}"
    az webapp deploy \
        --resource-group $RESOURCE_GROUP \
        --name $APP_SERVICE_NAME \
        --src-path ./app.zip \
        --type zip
    
    echo -e "\n${GREEN}========================================${NC}"
    echo -e "${GREEN}Deployment Complete!${NC}"
    echo -e "${GREEN}========================================${NC}"
    echo -e "${YELLOW}App URL: https://$(az webapp show --name $APP_SERVICE_NAME --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv)/Index${NC}"
    echo -e "${YELLOW}Note: Navigate to /Index to view the application${NC}"
else
    echo -e "\n${YELLOW}Warning: app.zip not found. Skipping application deployment.${NC}"
    echo -e "${YELLOW}Build the application first, then run: az webapp deploy --resource-group $RESOURCE_GROUP --name $APP_SERVICE_NAME --src-path ./app.zip${NC}"
fi

echo -e "\n${GREEN}For local development:${NC}"
echo -e "${YELLOW}1. Run 'az login' to authenticate${NC}"
echo -e "${YELLOW}2. Update appsettings.Development.json with: \"Authentication=Active Directory Default\"${NC}"
echo -e "${YELLOW}3. Run the application locally${NC}"
