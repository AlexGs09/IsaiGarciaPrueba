var passwords = new[]
{
    "ClaveActiva1!",
    "ClaveTemp1!",
    "ClaveSistema1!",
    "ClaveInactiva1!"
};

foreach (var password in passwords)
{
    Console.WriteLine($"{password} => {BCrypt.Net.BCrypt.HashPassword(password)}");
}
