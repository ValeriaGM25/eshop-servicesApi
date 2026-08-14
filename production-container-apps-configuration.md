# Production Container Apps Configuration

This backend expects production configuration from Azure Container Apps environment variables and secrets. Do not store real PostgreSQL connection strings, MongoDB connection strings, JWT keys, admin passwords, Azure credentials, or API keys in repository files.

All four APIs must use exactly the same JWT issuer, audience, and signing key.

```text
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
```

## Identity API

Container App: `eshop-identity-api`

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__IdentityDatabase=secretref:identity-db
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7
AUTH_ADMIN_EMAIL=<configurable>
AUTH_ADMIN_PASSWORD=secretref:admin-password
AUTH_ADMIN_FULL_NAME=<configurable>
AUTH_ADMIN_FORCE_PASSWORD_RESET=false
Cors__AllowedOrigins__0=https://eshop-services.netlify.app
```

## Catalog API

Container App: `eshop-catalog-api`

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__Database=secretref:catalog-db
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
Cors__AllowedOrigins__0=https://eshop-services.netlify.app
DatabaseInitialization__SeedDemoData=false
```

`DatabaseInitialization__SeedDemoData` defaults to `false` in Production. Set it to `true` only when demo catalog data should be inserted.

## Basket API

Container App: `eshop-basket-api`

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__Database=secretref:basket-db
ConnectionStrings__Redis=eshop-redis:6379,abortConnect=false
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
Cors__AllowedOrigins__0=https://eshop-services.netlify.app
```

Redis is expected to be reachable inside Azure Container Apps at `eshop-redis:6379`. The application also enforces resilient Redis client options at startup.

## Orders API

Container App: `eshop-orders-api`

Required variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080

MongoDb__ConnectionString=secretref:mongodb-connection
MongoDb__DatabaseName=EshopOrders
MongoDb__OrdersCollection=orders

Orders__TaxRate=0.18

Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key

BasketApi__BaseAddress=http://eshop-basket-api
CatalogApi__BaseAddress=http://eshop-catalog-api

Cors__AllowedOrigins__0=https://eshop-services.netlify.app
```

Expected Orders secrets:

```text
mongodb-connection
jwt-key
```

Orders uses MongoDB Atlas for persistence. Keep:

```text
MongoDb__DatabaseName=EshopOrders
MongoDb__OrdersCollection=orders
```

Orders creates/verifies these MongoDB indexes at startup:

```text
ux_orders_idempotency_key
ix_orders_customer_id
ix_orders_created_at
```

Orders communicates with Basket and Catalog only over HTTP using `BasketApi__BaseAddress` and `CatalogApi__BaseAddress`. It forwards the user's Bearer token to Basket.

## Health Checks

Each API exposes:

```text
GET /health/live
GET /health/ready
GET /health
```

`/health/live` only indicates that ASP.NET Core is alive. `/health/ready` and `/health` depend on service readiness and configured dependencies. Orders readiness checks MongoDB Atlas without exposing the connection string.

## Docker Image Commands

Build from the repository root. Use the current Git short SHA as the image tag.

```powershell
$TAG = git rev-parse --short HEAD
$ACR = "eshopvaleria2026acr.azurecr.io"

docker build --platform linux/amd64 -f "Identity/Identity.API/Dockerfile" -t "identity-api:$TAG" .
docker build --platform linux/amd64 -f "Catalog.AP/Dockerfile" -t "catalog-api:$TAG" .
docker build --platform linux/amd64 -f "Basket/Basket/Dockerfile" -t "basket-api:$TAG" .
docker build --platform linux/amd64 -f "Orders/Orders.API/Dockerfile" -t "orders-api:$TAG" .

docker tag "identity-api:$TAG" "$ACR/identity-api:$TAG"
docker tag "catalog-api:$TAG" "$ACR/catalog-api:$TAG"
docker tag "basket-api:$TAG" "$ACR/basket-api:$TAG"
docker tag "orders-api:$TAG" "$ACR/orders-api:$TAG"
```

Push only during an approved deployment:

```powershell
docker push "$ACR/identity-api:$TAG"
docker push "$ACR/catalog-api:$TAG"
docker push "$ACR/basket-api:$TAG"
docker push "$ACR/orders-api:$TAG"
```

## Suggested Container App Updates

Do not delete or recreate existing Container Apps. Updating image or environment variables creates a new Azure Container Apps revision.

Existing apps to update:

```text
eshop-identity-api
eshop-catalog-api
eshop-basket-api
```

New app to create:

```text
eshop-orders-api
```

Suggested commands only. Do not paste real secrets into documentation or source control.

```powershell
$RESOURCE_GROUP = "rg-eshop-production"
$ENVIRONMENT = "eshop-production-env"
$ACR = "eshopvaleria2026acr.azurecr.io"
$TAG = git rev-parse --short HEAD
```

Update existing Container Apps with new revisions:

```powershell
az containerapp update `
  --name eshop-identity-api `
  --resource-group $RESOURCE_GROUP `
  --image "$ACR/identity-api:$TAG" `
  --set-env-vars `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ASPNETCORE_HTTP_PORTS=8080" `
    "Jwt__Issuer=eshop-identity-production" `
    "Jwt__Audience=eshop-apis-production" `
    "Jwt__Key=secretref:jwt-key" `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

```powershell
az containerapp update `
  --name eshop-catalog-api `
  --resource-group $RESOURCE_GROUP `
  --image "$ACR/catalog-api:$TAG" `
  --set-env-vars `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ASPNETCORE_HTTP_PORTS=8080" `
    "Jwt__Issuer=eshop-identity-production" `
    "Jwt__Audience=eshop-apis-production" `
    "Jwt__Key=secretref:jwt-key" `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app" `
    "DatabaseInitialization__SeedDemoData=false"
```

```powershell
az containerapp update `
  --name eshop-basket-api `
  --resource-group $RESOURCE_GROUP `
  --image "$ACR/basket-api:$TAG" `
  --set-env-vars `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ASPNETCORE_HTTP_PORTS=8080" `
    "Jwt__Issuer=eshop-identity-production" `
    "Jwt__Audience=eshop-apis-production" `
    "Jwt__Key=secretref:jwt-key" `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

Create Orders Container App after its image and secrets are available:

```powershell
az containerapp create `
  --name eshop-orders-api `
  --resource-group $RESOURCE_GROUP `
  --environment $ENVIRONMENT `
  --image "$ACR/orders-api:$TAG" `
  --target-port 8080 `
  --ingress external `
  --registry-server $ACR `
  --registry-identity "/subscriptions/<subscription-id>/resourceGroups/rg-eshop-production/providers/Microsoft.ManagedIdentity/userAssignedIdentities/eshop-acr-pull" `
  --secrets `
    "mongodb-connection=<MONGODB_ATLAS_CONNECTION_STRING>" `
    "jwt-key=<JWT_SIGNING_KEY>" `
  --env-vars `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ASPNETCORE_HTTP_PORTS=8080" `
    "MongoDb__ConnectionString=secretref:mongodb-connection" `
    "MongoDb__DatabaseName=EshopOrders" `
    "MongoDb__OrdersCollection=orders" `
    "Orders__TaxRate=0.18" `
    "Jwt__Issuer=eshop-identity-production" `
    "Jwt__Audience=eshop-apis-production" `
    "Jwt__Key=secretref:jwt-key" `
    "BasketApi__BaseAddress=http://eshop-basket-api" `
    "CatalogApi__BaseAddress=http://eshop-catalog-api" `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

If Orders is reached only through another backend gateway, change `--ingress external` to the approved internal ingress strategy. The current frontend flow requires Orders to be reachable from Netlify.

## Initial Administrator Password Reset

The initial administrator is identified by `AUTH_ADMIN_EMAIL`. The password is read only from `AUTH_ADMIN_PASSWORD`, which should point to the `admin-password` Container App secret in production.

Changing the secret value does not automatically update the stored ASP.NET Core Identity password hash. To rotate the initial administrator password in a controlled way:

1. Update the `admin-password` secret.
2. Set `AUTH_ADMIN_FORCE_PASSWORD_RESET=true`.
3. Create a new revision or restart the revision.
4. Wait for the log `Initial admin password reset completed.`
5. Test login with the new password.
6. Set `AUTH_ADMIN_FORCE_PASSWORD_RESET=false` again.
7. Create a new revision.
