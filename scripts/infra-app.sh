#!/bin/bash
#
# Cria a infraestrutura do PetCareAI na Azure.
# Executado pelo stage "Infraestrutura" do azure-pipelines.yml, que passa os parâmetros abaixo.
# Os comandos podem rodar de novo sem problema: se o recurso já existe, ele é mantido.
#

# Parâmetros (variáveis do pipeline)
WEBAPP_NAME=$1
SQL_SERVER=$2
SQL_DB=$3
SQL_ADMIN_USER=$4
SQL_ADMIN_PASS=$5

# Variáveis
RESOURCE_GROUP="rg-petcareai-devops"
LOCATION_SQL="centralus"
LOCATION_APP="westus3"          # única região com cota de App Service na assinatura
LOCATION_INSIGHTS="eastus"
APP_SERVICE_PLAN="asp-petcareai-devops"
APP_INSIGHTS_NAME="appi-petcareai"
RUNTIME="DOTNETCORE:8.0"


# Registrar os Serviços utilizados
az provider register --namespace Microsoft.Web --wait
az provider register --namespace Microsoft.Sql --wait
az provider register --namespace Microsoft.Insights --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az extension add --name application-insights

# Criação do Grupo de Recursos
az group create --name $RESOURCE_GROUP --location $LOCATION_SQL

# Banco de Dados Azure SQL (PaaS)
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION_SQL \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASS"

az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DB \
  --service-objective Basic \
  --backup-storage-redundancy Local

# Libera o acesso ao banco para os serviços da Azure (Web App)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Criar Application Insights
az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION_INSIGHTS \
  --resource-group $RESOURCE_GROUP \
  --application-type web

# Criar o Plano de Serviço (F1 gratuito: a assinatura não tem cota para B1) e o Serviço de Aplicativo
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION_APP \
  --sku F1 \
  --is-linux

az webapp create \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "$RUNTIME"

# Conectar o Web App ao Application Insights
CONNECTION_STRING=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

az webapp config appsettings set \
  --name $WEBAPP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$CONNECTION_STRING" \
    ApplicationInsightsAgent_EXTENSION_VERSION="~3"
