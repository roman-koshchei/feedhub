using System.Reflection;
using System.Runtime.InteropServices;

namespace Lib;

/// <summary>
/// Helper to safely use Env variables
/// </summary>
public class Env
{
    private static INewEnvStrategy strategy = new NewEnvThrowStrategy();

    /// <summary>
    /// Load environment variables from file if file exists.
    /// If file doesn't exist then it just do nothing.
    /// <param name="path">Path to environment file.</param>
    /// </summary>
    public static void LoadFile(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            int separatorIndex = line.IndexOf('=');

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>
    /// Run static initialization for all types marked with [Env].
    /// Save exception appeared during accessing env variables into list.
    /// </summary>
    /// <returns>Exceptions appeared during initialization of Env classes</returns>
    public static List<Exception> Ensure()
    {
        var collectStrategy = new NewEnvCollectStrategy();
        strategy = collectStrategy;

        var types = AppDomain.CurrentDomain
           .GetAssemblies()
           .SelectMany(x => x
               .GetTypes()
               .Where(type => type.GetCustomAttribute<EnvAttribute>() != null)
           );

        foreach (var type in types)
        {
            try { type.TypeInitializer?.Invoke(null, null); }
            catch (Exception error) { collectStrategy.Errors.Add(error); }
        }

        return collectStrategy.Errors;
    }

    /// <summary>
    /// Print errors appeared during Env initialization in a beatiful way.
    /// </summary>
    public static void Describe(List<Exception> errors)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Errors with ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Environment variables");
        Console.ForegroundColor = ConsoleColor.White;

        int i = 0;
        foreach (var err in errors)
        {
            Console.CursorLeft = 4;
            Console.WriteLine($"{i}: {err.Message}");
            i += 1;
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Gets the required environment variable. <para />
    /// If you used <see cref="Ensure"/> then exception will be collected
    /// into the list returned from <see cref="Ensure"/>. <para />
    /// Otherwise, the method will throw <see cref="KeyNotFoundException"/>.
    /// </summary>
    public static string GetRequired(string key)
    {
        return strategy.GetRequired(key);
    }

    /// <summary>Get optional environment variable.</summary>
    /// <returns>Null if not found, otherwise value</returns>
    public static string? GetOptional(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    public static T GetRequired<T>(string key) where T : IParsable<T>
    {
        return strategy.GetRequired<T>(key);
    }

    /// <summary>Get optional environment variable of class <typeparamref name="T"/>.</summary>
    /// <returns>Null if not found, otherwise value</returns>
    public static T? GetOptionalRef<T>(string key) where T : class, IParsable<T>
    {
        var value = GetOptional(key);
        if (value == null) return null;
        try
        {
            return T.Parse(value, null);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Get optional environment variable of value struct <typeparamref name="T"/>.</summary>
    /// <returns>Null if not found, otherwise value</returns>
    public static T? GetOptionalVal<T>(string key) where T : struct, IParsable<T>
    {
        var value = GetOptional(key);
        if (value == null) return null;
        try
        {
            return T.Parse(value, null);
        }
        catch
        {
            return null;
        }
    }
}

internal interface INewEnvStrategy
{
    public string GetRequired(string key);

    public T GetRequired<T>(string key) where T : IParsable<T>;
}

internal class NewEnvThrowStrategy : INewEnvStrategy
{
    public string GetRequired(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new KeyNotFoundException($"Env variable '{key}' isn't found");
    }

    public T GetRequired<T>(string key) where T : IParsable<T>
    {
        var value = GetRequired(key);
        try
        {
            return T.Parse(value, null);
        }
        catch (Exception ex)
        {
            throw new FormatException(
                $"Environment variable '{key}' can't be parsed into {nameof(T)} type!", ex
            );
        }
    }
}

internal class NewEnvCollectStrategy : INewEnvStrategy
{
    public List<Exception> Errors { get; } = [];

    public string GetRequired(string key)
    {
        var env = Environment.GetEnvironmentVariable(key);
        if (env == null)
        {
            Errors.Add(new KeyNotFoundException($"Env variable '{key}' isn't found"));
            return "";
        }
        return env;
    }

    public T GetRequired<T>(string key) where T : IParsable<T>
    {
        var env = Environment.GetEnvironmentVariable(key);
        if (env == null)
        {
            Errors.Add(new KeyNotFoundException($"Env variable '{key}' isn't found"));
#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }

        try
        {
            return T.Parse(env, null);
        }
        catch (Exception ex)
        {
            Errors.Add(new FormatException(
                $"Environment variable '{key}' can't be parsed into {nameof(T)} type!", ex
            ));
#pragma warning disable CS8603 // Possible null reference return.
            return default;
#pragma warning restore CS8603 // Possible null reference return.
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class EnvAttribute : Attribute
{
}