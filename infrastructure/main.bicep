// Main Bicep orchestration file
targetScope = 'resourceGroup'

param location string = 'uksouth'
param deployGenAI bool = false
param adminLogin string
param adminObjectId string

var uniqueSuffix = uniqueString(resourceGroup().id)
var appName = 'app-expense-${uniqueSuffix}'
var managedIdentityName = 'mid-AppModAssist-${uniqueSuffix}'
var sqlServerName = 'sql-expense-${uniqueSuffix}'
var openAIName = 'openai-expense-${uniqueSuffix}'
var searchName = 'search-expense-${uniqueSuffix}'

// Deploy App Service with Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appName: appName
    managedIdentityName: managedIdentityName
  }
}

// Deploy Azure SQL Database
module azureSQL 'azure-sql.bicep' = {
  name: 'azureSQLDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: 'ExpenseManagement'
    adminLogin: adminLogin
    adminObjectId: adminObjectId
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Conditionally deploy GenAI resources
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    openAIName: openAIName
    searchName: searchName
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceHostName string = appService.outputs.appServiceHostName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output databaseName string = azureSQL.outputs.databaseName
output sqlServerName string = azureSQL.outputs.sqlServerName

// Conditional GenAI outputs with null safety
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''
