using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using MediatR;

namespace Auth.Application.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Email zaten kayıtlı mı kontrol et
        var exists = await _userRepository.ExistsAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"'{request.Email}' adresi ile kayıtlı bir kullanıcı zaten mevcut.");
        }

        // Password'ü hashle
        var passwordHash = _passwordHasher.Hash(request.Password);

        // User entity oluştur
        var user = User.Create(request.Email, request.FullName, passwordHash);

        // Veritabanına kaydet
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(user.Id, user.Email);
    }
}
