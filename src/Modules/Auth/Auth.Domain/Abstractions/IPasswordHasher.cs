namespace Auth.Domain.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);

    /// <summary>
    /// Sabit (dummy) bir hash'e karşı doğrulama yapar. Kullanıcı bulunamadığında çağrılır;
    /// böylece yanıt süresi gerçek doğrulamayla aynı kalır ve email enumeration engellenir.
    /// </summary>
    void VerifyDummy(string password);
}
