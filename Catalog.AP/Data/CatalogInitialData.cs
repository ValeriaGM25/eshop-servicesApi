namespace Catalog.API.Data
{
    public static class CatalogInitialData
    {
        public static bool ShouldSeedDemoData(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configuredValue = configuration["DatabaseInitialization:SeedDemoData"];
            if (bool.TryParse(configuredValue, out var seedDemoData))
            {
                return seedDemoData;
            }

            return environment.IsDevelopment();
        }

        public static async Task SeedAsync(IServiceProvider services, bool createScope = true)
        {
            using var scope = createScope ? services.CreateScope() : null;
            var serviceProvider = scope?.ServiceProvider ?? services;
            var session = serviceProvider.GetRequiredService<IDocumentSession>();

            await NormalizeDemoProductNamesAsync(session);
            var existingNames = (await session.Query<Product>().ToListAsync())
                .Select(product => product.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var product in GetDemoProducts().Where(product => !existingNames.Contains(product.Name)))
            {
                session.Store(product);
            }

            await session.SaveChangesAsync();
        }

        private static async Task NormalizeDemoProductNamesAsync(IDocumentSession session)
        {
            var replacements = new Dictionary<string, string>
            {
                ["Mouse Inalambrico"] = "Mouse Inalámbrico",
                ["Teclado Mecanico RGB"] = "Teclado Mecánico RGB",
                ["Audifonos Bluetooth"] = "Audífonos Bluetooth",
                ["Silla Gamer Ergonomica"] = "Silla Gamer Ergonómica"
            };

            var products = (await session.Query<Product>().ToListAsync())
                .Where(product => replacements.ContainsKey(product.Name))
                .ToList();

            if (products.Count == 0)
            {
                return;
            }

            foreach (var product in products)
            {
                product.Name = replacements[product.Name];
                session.Store(product);
            }

            await session.SaveChangesAsync();
        }

        public static IEnumerable<Product> GetDemoProducts() =>
        [
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Laptop Pro 14",
                Description = "Laptop profesional de 14 pulgadas para productividad.",
                Category = ["Computadoras", "Laptops"],
                ImageFiles = "https://placehold.co/600x400?text=Laptop+Pro+14",
                Price = 1299.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Mouse Inalámbrico",
                Description = "Mouse ergonomico inalambrico para trabajo diario.",
                Category = ["Accesorios", "Perifericos"],
                ImageFiles = "https://placehold.co/600x400?text=Mouse+Inalambrico",
                Price = 29.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Teclado Mecánico RGB",
                Description = "Teclado mecanico con iluminacion RGB.",
                Category = ["Accesorios", "Perifericos"],
                ImageFiles = "https://placehold.co/600x400?text=Teclado+Mecanico+RGB",
                Price = 89.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Monitor 27 Pulgadas",
                Description = "Monitor de 27 pulgadas para oficina y gaming casual.",
                Category = ["Monitores"],
                ImageFiles = "https://placehold.co/600x400?text=Monitor+27+Pulgadas",
                Price = 249.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Audífonos Bluetooth",
                Description = "Audifonos bluetooth con cancelacion de ruido.",
                Category = ["Audio", "Accesorios"],
                ImageFiles = "https://placehold.co/600x400?text=Audifonos+Bluetooth",
                Price = 79.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Webcam Full HD",
                Description = "Webcam 1080p para reuniones y streaming.",
                Category = ["Accesorios", "Video"],
                ImageFiles = "https://placehold.co/600x400?text=Webcam+Full+HD",
                Price = 49.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Disco SSD 1TB",
                Description = "Unidad SSD de 1TB para alto rendimiento.",
                Category = ["Almacenamiento"],
                ImageFiles = "https://placehold.co/600x400?text=Disco+SSD+1TB",
                Price = 109.99m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Silla Gamer Ergonómica",
                Description = "Silla ergonomica para largas jornadas de trabajo o juego.",
                Category = ["Muebles", "Gaming"],
                ImageFiles = "https://placehold.co/600x400?text=Silla+Gamer+Ergonomica",
                Price = 199.99m
            }
        ];
    }
}
