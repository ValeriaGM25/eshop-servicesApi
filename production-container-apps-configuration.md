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
Cors__AllowedOrigins__0=https://DOMINIO-NETLIFY
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
Cors__AllowedOrigins__0=https://DOMINIO-NETLIFY
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
Cors__AllowedOrigins__0=https://DOMINIO-NETLIFY
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

## Suggested Image Commands

These commands build local v2 images only. They do not push to ACR.

```powershell
docker build -f "Identity/Identity.API/Dockerfile" -t identity-api:v2 .
docker build -f "Catalog.AP/Dockerfile" -t catalog-api:v2 .
docker build -f "Basket/Basket/Dockerfile" -t basket-api:v2 .
```

Tag for ACR only when ready to deploy:

```powershell
docker tag identity-api:v2 eshopvaleria2026acr.azurecr.io/identity-api:v2
docker tag catalog-api:v2 eshopvaleria2026acr.azurecr.io/catalog-api:v2
docker tag basket-api:v2 eshopvaleria2026acr.azurecr.io/basket-api:v2
```

Do not run `docker push` until deployment is explicitly approved.
