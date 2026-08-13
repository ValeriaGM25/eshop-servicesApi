# eshop-services Backend

Backend de microservicios ASP.NET Core Minimal APIs para e-commerce.

## Arquitectura

- `Catalog.API`: catálogo de productos, PostgreSQL mediante Marten.
- `Basket`: carrito de compras, PostgreSQL mediante Marten y Redis para caché.
- `Identity.API`: autenticación, PostgreSQL, ASP.NET Core Identity y JWT.
- `Orders.API`: órdenes de compra, MongoDB Atlas mediante `MongoDB.Driver`.
- `BuildingBlocks`: CQRS, excepciones, JWT/CORS y health checks compartidos.

MongoDB Atlas se usa únicamente para `Orders.API`. Catalog, Basket e Identity conservan sus persistencias actuales.

## Orders.API

`Orders.API` es un microservicio independiente para crear, consultar, actualizar estado y generar reportes PDF de órdenes.

Persistencia:

- Base de datos: `EshopOrders`.
- Colección: `orders`.
- Driver: `MongoDB.Driver`.
- No existe contenedor MongoDB local en Docker Compose; Atlas es obligatorio para evidencia final.

Índices creados al iniciar:

- `ux_orders_idempotency_key`: único sobre `IdempotencyKey`.
- `ix_orders_customer_id`: consulta por cliente.
- `ix_orders_created_at`: ordenamiento por fecha.

## Variables De Entorno

Requeridas para `Orders.API`:

```bash
MongoDb__ConnectionString=<mongodb-atlas-connection-string>
MongoDb__DatabaseName=EshopOrders
MongoDb__OrdersCollection=orders
Orders__TaxRate=0.18
Jwt__Issuer=eshop.identity
Jwt__Audience=eshop.apis
Jwt__Key=<long-random-secret>
BasketApi__BaseAddress=http://localhost:6001
CatalogApi__BaseAddress=http://localhost:6002
Cors__AllowedOrigins__0=http://localhost:5173
Cors__AllowedOrigins__1=https://your-site.netlify.app
```

En Docker Compose se usan placeholders seguros desde `.env.example`:

```bash
MONGODB_CONNECTION_STRING=CHANGE_ME_WITH_MONGODB_ATLAS_CONNECTION_STRING
MONGODB_DATABASE_NAME=EshopOrders
MONGODB_ORDERS_COLLECTION=orders
ORDERS_TAX_RATE=0.18
NETLIFY_ORIGIN=https://your-site.netlify.app
```

No guardar connection strings reales, contraseñas o secretos en `appsettings.json`, Dockerfile, Docker Compose, README o Git.

## Impuestos Y Totales

`Orders.API` calcula en backend:

- `Subtotal`: suma de `Quantity * UnitPrice`.
- `Tax`: `Subtotal * Orders__TaxRate`.
- `Total`: `Subtotal + Tax`.

La tasa por defecto documentada es `0.18` y debe configurarse con `Orders__TaxRate` o `ORDERS_TAX_RATE` en Docker Compose. React no debe enviar ni controlar los totales.

La orden conserva el precio usado al comprar. Las órdenes históricas no se recalculan si Catalog cambia después.

## Endpoints

Todos requieren JWT salvo health/OpenAPI.

### POST `/api/orders`

Crea una orden desde el carrito autenticado.

Header requerido:

```http
Idempotency-Key: unique-key-123
Authorization: Bearer <token>
```

Body compatible con el contrato del examen:

```json
{
  "customerId": "string",
  "basketId": "string"
}
```

Decisión de integración: Basket actual no usa un `basketId` independiente. `BasketId` se acepta por compatibilidad, pero el carrito real se obtiene llamando `GET /basket` con el JWT reenviado. El `CustomerId` efectivo se toma del JWT; si el body intenta usar otro customerId, se rechaza.

Respuestas:

- `201 Created`: primera creación.
- `200 OK`: replay idempotente con la misma `Idempotency-Key`; devuelve el mismo `Order.Id`.
- `400 Bad Request`: basket vacío, producto inválido, cantidad/precio inválido, producto inexistente.
- `401 Unauthorized` o `403 Forbidden`: token/rol inválido.
- `500 Internal Server Error`: error controlado de dependencias o MongoDB, sin secretos ni stack trace.

### GET `/api/orders/{id}`

Devuelve una orden completa. `Cliente` solo puede consultar sus órdenes. `Admin` puede consultar cualquiera.

### GET `/api/orders`

Lista órdenes para gestión administrativa. Requiere rol `Admin`.

Filtros opcionales:

- `status`: `Pending`, `Confirmed` o `Cancelled`.
- `customerId`: filtra por cliente.
- `search`: busca solo en campos existentes de Order: `Id`, `CustomerId`, `ProductName` y `ProductId` cuando el valor es un GUID.
- `from`: fecha UTC mínima de creación.
- `to`: fecha UTC máxima de creación.
- `page`: página, mínimo `1`.
- `pageSize`: tamaño de página, máximo `100`.

Ejemplo:

```http
GET /api/orders?status=Pending&page=1&pageSize=10
```

Respuesta paginada con registros resumidos:

```json
{
  "items": [
    {
      "id": "string",
      "customerId": "string",
      "createdAt": "2026-08-13T00:00:00Z",
      "status": "Pending",
      "itemsCount": 2,
      "subtotal": 100,
      "tax": 18,
      "total": 118
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1
}
```

No devuelve email ni nombre del cliente porque esos datos no existen en `Order`.

### GET `/api/orders/customer/{customerId}`

Devuelve órdenes de un cliente. `Cliente` solo puede consultar su propio `customerId`. `Admin` puede consultar cualquiera.

Parámetros opcionales compatibles con la pantalla “Mis órdenes”:

- `status`
- `search`
- `from`
- `to`
- `page`
- `pageSize`

La respuesta conserva la forma de lista para no romper compatibilidad con clientes existentes.

### PATCH `/api/orders/{id}/status`

Actualiza estado. Requiere rol `Admin`.

Body:

```json
{
  "status": "Confirmed"
}
```

Transiciones válidas:

- `Pending -> Confirmed`
- `Pending -> Cancelled`

Transiciones inválidas devuelven `409 Conflict`.

### GET `/api/orders/{id}/report`

Genera el PDF en backend con `QuestPDF` y licencia `Community` configurada en código. Validar la licencia de QuestPDF contra el contexto real de uso antes de producción. Devuelve:

- `Content-Type: application/pdf`
- `Content-Disposition` con nombre `Orden-{OrderId}.pdf`

Incluye id de orden, cliente, fecha, estado, productos, subtotales, impuesto y total.

## Integración Basket

`Orders.API` no lee PostgreSQL ni Redis de Basket. Consume Basket por HTTP:

- `GET /basket` antes de crear la orden.
- `DELETE /basket` después de persistir correctamente la orden.

El Bearer token recibido por `Orders.API` se reenvía a Basket. Si el borrado falla después de guardar la orden, se registra el problema y la idempotencia evita duplicados en reintentos.

## Integración Catalog

Por cada `ProductId` del Basket, `Orders.API` llama:

```http
GET /products/{id}
```

Si Catalog devuelve `404`, la creación de orden responde `400 Bad Request`. El precio persistido se toma del Basket como precio de compra y queda congelado en la orden.

## Health Checks

- `/health/live`
- `/health/ready`
- `/health`

Readiness valida conectividad a MongoDB Atlas con `ping` sin exponer connection string.

## Ejecución Local

Restaurar y compilar:

```bash
dotnet restore
dotnet build
```

Ejecutar Orders localmente:

```bash
dotnet run --project Orders/Orders.API/Orders.API.csproj
```

Docker Compose:

```bash
docker compose up --build
```

`orders.api` publica `6004:8080` y usa internamente:

- Basket: `http://basket.api:8080`
- Catalog: `http://catalog.api:8080`

OpenAPI:

```http
GET http://localhost:6004/openapi/v1.json
```

## Pruebas

```bash
dotnet test
```

Cobertura agregada para Orders:

- orden válida crea documento lógico y calcula totales;
- consulta por id devuelve datos completos;
- basket vacío devuelve error;
- replay de `Idempotency-Key` devuelve misma orden sin duplicar;
- `Pending -> Confirmed`;
- `Pending -> Cancelled`;
- transición inválida;
- Admin consulta cualquier orden;
- Admin lista órdenes;
- filtro `Pending`;
- MongoDB no disponible genera error controlado sin secretos;
- reporte PDF existente genera bytes `application/pdf`;
- reporte de orden inexistente devuelve flujo 404 mediante query.

## Ejemplos Manuales

Crear orden:

```bash
curl -i -X POST http://localhost:6004/api/orders \
  -H "Authorization: Bearer <token-cliente>" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: order-demo-001" \
  -d '{"customerId":"<customer-id-del-jwt>","basketId":"compat-exam"}'
```

Repetir idempotencia:

```bash
curl -i -X POST http://localhost:6004/api/orders \
  -H "Authorization: Bearer <token-cliente>" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: order-demo-001" \
  -d '{"customerId":"<customer-id-del-jwt>","basketId":"compat-exam"}'
```

Consultar orden:

```bash
curl -i -H "Authorization: Bearer <token>" http://localhost:6004/api/orders/<order-id>
```

Confirmar orden:

```bash
curl -i -X PATCH http://localhost:6004/api/orders/<order-id>/status \
  -H "Authorization: Bearer <token-admin>" \
  -H "Content-Type: application/json" \
  -d '{"status":"Confirmed"}'
```

Descargar PDF:

```bash
curl -L -o Orden.pdf -H "Authorization: Bearer <token>" http://localhost:6004/api/orders/<order-id>/report
```

## Publicación

Configurar secretos en el proveedor de hosting, no en Git:

- MongoDB Atlas connection string.
- JWT issuer/audience/key.
- Basket/Catalog base addresses públicos o privados según despliegue.
- CORS para Netlify vía variable.

No hacer `git push` ni deploy automáticamente desde esta preparación.
