namespace Dentalara.Helpers
{
    public static class TokenHelper
    {
        public static string GenerarToken()
        {
            // Genera un token numérico de 6 dígitos (como Gmail)
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
    }
}