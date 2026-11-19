using System.Security.Cryptography;
using DragonKnightServer.Database;

namespace DragonKnightServer;

internal static class LocalPasswordReset
{
    public static int Run(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Укажите логин и запустите команду в интерактивном терминале.");
            return 1;
        }

        Span<byte> password = stackalloc byte[128];
        Span<byte> confirmation = stackalloc byte[128];
        try
        {
            int length = ReadPassword("Новый локальный пароль (печатные символы ASCII): ", password);
            int confirmedLength = ReadPassword("Повторите пароль: ", confirmation);
            if (length <= 0 || confirmedLength != length
                || !CryptographicOperations.FixedTimeEquals(password[..length], confirmation[..confirmedLength]))
            {
                Console.Error.WriteLine("Пароли должны совпадать и содержать от 1 до 128 печатных символов ASCII.");
                return 1;
            }

            // Старый клиент присылает 32 шестнадцатеричных символа MD5 после '!'.
            string passwordHash = Convert.ToHexString(MD5.HashData(password[..length])).ToLowerInvariant();
            if (!AccountRepository.ResetPassword(username, passwordHash))
            {
                Console.Error.WriteLine("Аккаунт с таким логином не найден.");
                return 1;
            }

            Console.WriteLine("Пароль локального аккаунта обновлён.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Смена пароля не выполнена: {ex.Message}");
            return 1;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
            CryptographicOperations.ZeroMemory(confirmation);
        }
    }

    private static int ReadPassword(string prompt, Span<byte> buffer)
    {
        Console.Write(prompt);
        int length = 0;
        bool invalid = false;
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return invalid ? -1 : length;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (length > 0)
                    buffer[--length] = 0;
            }
            else if (key.KeyChar is >= ' ' and <= '~' && length < buffer.Length)
            {
                buffer[length++] = (byte)key.KeyChar;
            }
            else if (key.KeyChar != '\0')
            {
                invalid = true;
            }
        }
    }
}
