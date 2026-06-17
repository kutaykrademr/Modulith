# Modulith — .NET 10 Modular Monolith Base API

Üretime hazır bir başlangıç noktası (template) olarak tasarlanmış, **vertical slice + Clean Architecture** prensipleriyle yazılmış bir modüler monolit. Auth ve User modülleri, klonlayıp kendi özelliklerinizi eklemeniz için referans niteliğindedir.

## Öne çıkanlar

- **.NET 10**, Minimal API, MediatR (CQRS), FluentValidation
- **Auth modülü:** register, login, email doğrulama, şifre sıfırlama, JWT access + refresh token (rotation + reuse detection)
- **User modülü:** profil; Auth'tan bağımsız, integration event ile beslenir
- **Tek paylaşılan PostgreSQL** + şema bazlı modül izolasyonu (`auth.*`, `user.*`)
- **Güvenlik:** BCrypt (workFactor 12), DB'de **hash'lenmiş** token'lar, email enumeration koruması, rate limiting, güvenlik header'ları, HSTS
- **EF Core Migrations**, health check (`/health`), Serilog, Scalar/OpenAPI UI
- **Entegrasyon testleri** (xUnit + WebApplicationFactory + Testcontainers)

## Hızlı başlangıç

```bash
# 1) Altyapı (PostgreSQL + pgAdmin + Mailpit)
docker compose up -d

# 2) Dev secret'ını ayarla (JWT imzalama anahtarı — repoda TUTULMAZ)
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)" \
  --project src/Host/Modulith.Api

# 3) Çalıştır (migration'lar otomatik uygulanır)
dotnet run --project src/Host/Modulith.Api

# Testler (Docker gerektirir)
dotnet test
```

**Dev URL'ler**
| | URL |
|---|---|
| API | http://localhost:5116 |
| Scalar (OpenAPI) | http://localhost:5116/scalar/v1 |
| Health | http://localhost:5116/health |
| pgAdmin | http://localhost:5050 (admin@modulith.dev / admin) |
| Mailpit | http://localhost:8025 |

## Yapılandırma & secret yönetimi

| Ayar | Dev | Prod |
|---|---|---|
| `Jwt:Secret` | `dotnet user-secrets set "Jwt:Secret" <değer>` | `Jwt__Secret` environment variable (min 32 karakter) |
| `ConnectionStrings:ModulithDb` | `appsettings.json` (docker varsayılanı) | `ConnectionStrings__ModulithDb` env var |
| `Cors:AllowedOrigins` | `appsettings.json` | env / appsettings.Production.json |

Açılışta kritik yapılandırma doğrulanır (`ConfigurationValidationExtensions`); secret eksik/kısa ise uygulama net bir hatayla durur.

### Veritabanı

Şema migration'larla yönetilir ve açılışta otomatik uygulanır. Dev'de sıfırdan başlamak için:

```bash
# appsettings.Development.json içine "Database": { "ResetOnStartup": true } ekleyin
# veya:
Database__ResetOnStartup=true dotnet run --project src/Host/Modulith.Api
```

Yeni migration:

```bash
dotnet ef migrations add <Ad> \
  --project src/Shared/Shared.Infrastructure \
  --startup-project src/Host/Modulith.Api \
  --output-dir Persistence/Migrations
```

## Yeni modül ekleme

1. `src/Modules/{Ad}/` altında Domain / Application / Infrastructure / Presentation projeleri oluşturun.
2. EF entity konfigürasyonlarını kendi DB şemanızla Infrastructure'a ekleyin (`ToTable("...", "{ad}")`).
3. `services.Add{Ad}Module(configuration)` kaydını ve `app.Map{Ad}Endpoints()` çağrısını `Program.cs`'e ekleyin.
4. Modüller arası iletişim için `Shared.Contracts`'ta `INotification` event tanımlayın, `IPublisher` ile yayınlayıp tüketen modülde `INotificationHandler<T>` ile dinleyin.
5. Projeleri `Modulith.slnx`'e ekleyin.

## Template'i kendi projenize uyarlama

`Modulith` adı çözüm, namespace ve klasörlere gömülüdür. Kendi adınıza geçmek için bu metni proje genelinde değiştirin (DB adı, JWT issuer/audience, email gönderen adresi dahil). `UserSecretsId` ve docker container/volume adlarını da güncelleyin.

## Mimari

Detaylar için [CLAUDE.md](CLAUDE.md).
