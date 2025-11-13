#!/bin/bash

RESOURCE_GROUP="rg-invoicerobot-prod"
LOCATION="northeurope"
SQL_PASSWORD="$(openssl rand -base64 32)"

# Luo Resource Group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy Bicep
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file main.bicep \
  --parameters \
    namePrefix=invoicerobot \
    environment=prod \
    accountingProvider=Netvisor \
    sqlServerPassword=$SQL_PASSWORD

echo "Deployment valmis!"
