# Production Container Apps Configuration

This backend expects production configuration from Azure Container Apps environment variables and secrets. Do not store real PostgreSQL connection strings, JWT keys, admin passwords, or Railway credentials in repository files.

All three APIs must use exactly the same JWT issuer, audience, and signing key.

## Identity API

Container App: `eshop-identity-api`

Required variables:

```text
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

Optional temporary legacy JWT fallbacks are still accepted during transition:

```text
JWT_ISSUER
JWT_AUDIENCE
JWT_KEY
JWT_ACCESS_TOKEN_MINUTES
JWT_REFRESH_TOKEN_DAYS
```

## Catalog API

Required variables:

```text
ConnectionStrings__Database=secretref:catalog-db
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
Cors__AllowedOrigins__0=https://eshop-services.netlify.app
DatabaseInitialization__SeedDemoData=false
```

`DatabaseInitialization__SeedDemoData` defaults to `false` in Production. Set it to `true` only when demo catalog data should be inserted.

## Basket API

Required variables:

```text
ConnectionStrings__Database=secretref:basket-db
ConnectionStrings__Redis=eshop-redis:6379,abortConnect=false
Jwt__Issuer=eshop-identity-production
Jwt__Audience=eshop-apis-production
Jwt__Key=secretref:jwt-key
Cors__AllowedOrigins__0=https://eshop-services.netlify.app
```

Redis is expected to be reachable inside Azure Container Apps at `eshop-redis:6379`. The application also enforces resilient Redis client options at startup.

## Health Checks

Each API exposes:

```text
GET /health/live
GET /health/ready
GET /health
```

`/health/live` only indicates that ASP.NET Core is alive. `/health/ready` and `/health` depend on service readiness and configured dependencies.

## Initial Administrator Password Reset

The initial administrator is identified by `AUTH_ADMIN_EMAIL`. The password is read only from `AUTH_ADMIN_PASSWORD`, which should point to the `admin-password` Container App secret in production.

First creation should use password reset disabled:

```text
AUTH_ADMIN_EMAIL=admin@gmail.com
AUTH_ADMIN_PASSWORD=secretref:admin-password
AUTH_ADMIN_FORCE_PASSWORD_RESET=false
```

Changing the secret value does not automatically update the stored ASP.NET Core Identity password hash. To rotate the initial administrator password in a controlled way:

1. Update the `admin-password` secret.
2. Set `AUTH_ADMIN_FORCE_PASSWORD_RESET=true`.
3. Create a new revision or restart the revision.
4. Wait for the log `Initial admin password reset completed.`
5. Test login with the new password.
6. Set `AUTH_ADMIN_FORCE_PASSWORD_RESET=false` again.
7. Create a new revision.

When the password reset completes, existing refresh tokens for the administrator are revoked. Existing access tokens can remain valid until they expire because the current API does not perform server-side JWT revocation.

Suggested commands only. Do not put a real password in documentation or source control.

```powershell
az containerapp secret set `
  --name eshop-identity-api `
  --resource-group rg-eshop-production `
  --secrets "admin-password=<VALOR-SEGURO>"
```

```powershell
az containerapp update `
  --name eshop-identity-api `
  --resource-group rg-eshop-production `
  --set-env-vars `
    "AUTH_ADMIN_PASSWORD=secretref:admin-password" `
    "AUTH_ADMIN_FORCE_PASSWORD_RESET=true"
```

After the reset is confirmed:

```powershell
az containerapp update `
  --name eshop-identity-api `
  --resource-group rg-eshop-production `
  --set-env-vars `
    "AUTH_ADMIN_FORCE_PASSWORD_RESET=false"
```

## Suggested Image Commands

These commands build local v3 images only. They do not push to ACR.

```powershell
docker build --platform linux/amd64 -f "Identity/Identity.API/Dockerfile" -t identity-api:v3 .
docker build --platform linux/amd64 -f "Catalog.AP/Dockerfile" -t catalog-api:v3 .
docker build --platform linux/amd64 -f "Basket/Basket/Dockerfile" -t basket-api:v3 .
```

Tag for ACR only when ready to deploy:

```powershell
docker tag identity-api:v3 eshopvaleria2026acr.azurecr.io/identity-api:v3
docker tag catalog-api:v3 eshopvaleria2026acr.azurecr.io/catalog-api:v3
docker tag basket-api:v3 eshopvaleria2026acr.azurecr.io/basket-api:v3
```

Push only when deployment is explicitly approved:

```powershell
docker push eshopvaleria2026acr.azurecr.io/identity-api:v3
docker push eshopvaleria2026acr.azurecr.io/catalog-api:v3
docker push eshopvaleria2026acr.azurecr.io/basket-api:v3
```

## Suggested Container App Updates

Updating environment variables creates a new Azure Container Apps revision. These commands are examples and should be run only during an approved deployment.

```powershell
az containerapp update `
  --name eshop-identity-api `
  --resource-group rg-eshop-production `
  --set-env-vars `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

```powershell
az containerapp update `
  --name eshop-catalog-api `
  --resource-group rg-eshop-production `
  --set-env-vars `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

```powershell
az containerapp update `
  --name eshop-basket-api `
  --resource-group rg-eshop-production `
  --set-env-vars `
    "Cors__AllowedOrigins__0=https://eshop-services.netlify.app"
```

If the CORS code changes are not already deployed, update each Container App image to the new image tag after building and pushing the approved images.
